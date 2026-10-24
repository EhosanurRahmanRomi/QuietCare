from pathlib import Path
import hashlib,json,subprocess,winreg

import argparse
parser=argparse.ArgumentParser(description="Validate only a fixture MSI in isolated directories.")
parser.add_argument('--package',required=True)
parser.add_argument('--output',required=True)
parser.add_argument('--upgrade-package')
args=parser.parse_args()
package=Path(args.package).resolve()
manifest=json.loads(package.with_suffix('.manifest.json').read_text())
if manifest.get('fixture') is not True:
    raise RuntimeError('Refusing a production package. Build with --fixture.')
area=Path(args.output).resolve()
if area.exists():
    raise RuntimeError('Use a fresh output directory for every native installer test.')
area.mkdir(parents=True)
install=area/'Installed';menu=area/'StartMenu';desktop=area/'Desktop'
for p in (install,menu,desktop):p.mkdir(exist_ok=True)
registry=r'Software\QuietCareInstallerTest06'
checks=[];failures=[]
def exists(key,name=None):
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key,0,winreg.KEY_READ|winreg.KEY_WOW64_64KEY) as k:
            return True if name is None else winreg.QueryValueEx(k,name)[0]
    except FileNotFoundError:return False
def verify(condition,label):
    if not condition:failures.append(label);print('FAIL:',label,flush=True);return
    checks.append(label);print('PASS:',label,flush=True)
def run(args,label,expected=(0,3010)):
    log=area/(label+'.log')
    # MSI parses NAME="value" itself; quoting the whole NAME=value token opens its usage dialog.
    def quote(arg):
        if '=' in arg:
            key,value=arg.split('=',1)
            return key+'="'+value+'"'
        return '"'+arg+'"' if ' ' in arg else arg
    if args[0]=='/x':args=[*args,'INSTALLDIR='+str(install),'APP_MENU='+str(menu),'APP_DESKTOP='+str(desktop)]
    command='"C:\\Windows\\System32\\msiexec.exe" '+' '.join(quote(a) for a in [*args,'/qn','/norestart','/L*v',str(log)])
    p=subprocess.run(command,timeout=90)
    if p.returncode not in expected:
        text=log.read_text(encoding='utf-16',errors='replace') if log.exists() else 'No MSI log created.'
        location=text.find('Return value 3')
        print(text[max(0,location-2200):location+200])
        raise RuntimeError(f'{label}: MSI exit {p.returncode}')
    verify(True,label+' native exit '+str(p.returncode))
def install_package(enabled,label):
    run(['/i',str(package),'INSTALLDIR='+str(install),'APP_MENU='+str(menu),'APP_DESKTOP='+str(desktop),'STARTUP='+str(int(enabled)),'DESKTOP='+str(int(enabled))],label)
run_args=manifest['productCode']
uninstall_key=r'Software\Microsoft\Windows\CurrentVersion\Uninstall'+'\\'+run_args
install_package(True,'install-on')
for f in manifest['files']:
    verify(hashlib.sha256((install/f['path']).read_bytes()).hexdigest()==f['sha256'],'Installed hash: '+f['path'])
verify(exists(uninstall_key,'DisplayName')=='QuietCare Installer Fixture','Windows installed-app registration')
verify(exists(uninstall_key,'WindowsInstaller')==1,'Windows native uninstaller registration')
verify(str(install) in str(exists(registry+r'\Run','QuietCareNext')) and '--tray --monitor' in str(exists(registry+r'\Run','QuietCareNext')),'Sign-in command targets installed executable')
verify(exists(registry+r'\StartupApproved','QuietCareNext')==bytes.fromhex('020000000000000000000000'),'Startup enabled state')
for p in [menu/'QuietCare.lnk',menu/'QuietCare Performance Monitor.lnk',desktop/'QuietCare.lnk']:
    verify(p.is_file(),'Shortcut: '+str(p.relative_to(area)))
# An unowned file must survive native uninstall.
keep=install/'unowned-project-sentinel.txt';keep.write_text('Keep this user-owned file.',encoding='utf-8')
run(['/x',run_args],'uninstall-on')
verify(all(not (install/f['path']).exists() for f in manifest['files']),'Uninstall removes package-owned files')
verify(keep.read_text()=='Keep this user-owned file.','Uninstall preserves unowned files')
verify(not exists(uninstall_key),'Uninstall removes app registration')
verify(not exists(registry+r'\Run','QuietCareNext'),'Uninstall removes owned startup command')
verify(not list(menu.glob('*.lnk')) and not list(desktop.glob('*.lnk')),'Uninstall removes owned shortcuts')
install_package(False,'install-off')
verify(not exists(registry+r'\Run','QuietCareNext'),'Startup unchecked is respected')
verify(not (desktop/'QuietCare.lnk').exists(),'Desktop unchecked is respected')
verify((menu/'QuietCare.lnk').exists(),'Start menu available without desktop shortcut')
run(['/x',run_args],'uninstall-off')
verify(keep.is_file(),'Second uninstall preserves unowned files')
if args.upgrade_package:
    upgraded=Path(args.upgrade_package).resolve()
    upgrade_manifest=json.loads(upgraded.with_suffix('.manifest.json').read_text())
    if upgrade_manifest.get('fixture') is not True or upgrade_manifest['upgradeCode']!=manifest['upgradeCode']:
        raise RuntimeError('Upgrade must be a fixture in the same test family.')
    install_package(True,'upgrade-baseline')
    properties=['INSTALLDIR='+str(install),'APP_MENU='+str(menu),'APP_DESKTOP='+str(desktop),'STARTUP=1','DESKTOP=1']
    run(['/i',str(upgraded),*properties],'upgrade-newer')
    upgraded_key=r'Software\Microsoft\Windows\CurrentVersion\Uninstall'+'\\'+upgrade_manifest['productCode']
    verify(not exists(uninstall_key),'Upgrade removes old product registration')
    verify(exists(upgraded_key,'DisplayName')=='QuietCare Installer Fixture','Upgrade registers new product')
    verify(keep.is_file(),'Upgrade preserves unowned files')
    run(['/i',str(package),*properties],'downgrade-rejected',expected=(1603,))
    verify(exists(upgraded_key),'Rejected downgrade preserves newer product')
    for f in upgrade_manifest['files']:
        verify(hashlib.sha256((install/f['path']).read_bytes()).hexdigest()==f['sha256'],'Upgraded file hash: '+f['path'])
    run(['/x',upgrade_manifest['productCode']],'uninstall-upgraded')
    verify(not exists(upgraded_key) and keep.is_file(),'Upgraded uninstall preserves unowned files')
receipt={'scope':'Native Windows MSI fixture install/uninstall; isolated directories and startup registry; no user startup changes or app launch','checks':checks,'failures':failures,'passed':len(checks),'packageSha256':hashlib.sha256(package.read_bytes()).hexdigest()}
(area/'installer-validation.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print('TOTAL',len(checks),flush=True)
assert not failures,failures
