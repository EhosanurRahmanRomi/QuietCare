"""Build a native, per-user Windows Installer package using Python's Windows MSI library.

Requires Python 3.12 on Windows. No downloaded installer engine, custom privileged actions,
remote bootstrap, recursive removal, or user-data deletion is used.
"""
from pathlib import Path
import argparse,hashlib,json,msilib,uuid,xml.etree.ElementTree as ET
from msilib import schema,sequence

parser=argparse.ArgumentParser();parser.add_argument('--payload',required=True);parser.add_argument('--output',required=True);parser.add_argument('--fixture',action='store_true');parser.add_argument('--version',default='0.6.0');args=parser.parse_args()
payload=Path(args.payload).resolve();out=Path(args.output).resolve();out.parent.mkdir(parents=True,exist_ok=True)
assert payload.is_dir() and (payload/'QuietCare.Next.exe').is_file()
version=args.version;family='QuietCare.FixtureV2' if args.fixture else 'QuietCare.Desktop'
metadata=ET.parse(Path(__file__).resolve().parents[1]/'src/Cleaner.App/Cleaner.App.csproj').getroot()
file_version=metadata.findtext('.//FileVersion')
parts=version.split('.')
if len(parts)!=3 or not all(part.isdigit() for part in parts) or not (int(parts[0])<256 and int(parts[1])<256 and int(parts[2])<65536):raise ValueError('Windows Installer version must be major.minor.build.')
guid=lambda value:'{'+str(uuid.uuid5(uuid.NAMESPACE_URL,'https://github.com/EhosanurRahmanRomi/QuietCare/'+family+'/'+value)).upper()+'}'
product=guid(version);upgrade=guid('upgrade')
db=msilib.init_database(str(out),schema,'QuietCare Installer Fixture' if args.fixture else 'QuietCare',product,version,'QuietCare contributors')
msilib.add_tables(db,sequence)
add=lambda table,rows:msilib.add_data(db,table,rows)
add('Property',[
 ('UpgradeCode',upgrade),('INSTALLLEVEL','1'),('STARTUP','1'),('DESKTOP','1'),('ARPNOMODIFY','1'),('ARPNOREPAIR','1'),
 ('ARPURLINFOABOUT','https://github.com/EhosanurRahmanRomi/QuietCare'),('ARPPRODUCTICON','QuietCareIcon'),('ARPSYSTEMCOMPONENT','1'),
 ('ARPCOMMENTS','Per-user Windows PC care. Uninstall preserves settings, recovery and startup backups.'),
 ('SecureCustomProperties','STARTUP;DESKTOP;OLDERPRODUCTS;NEWERPRODUCTS'),('MSIRESTARTMANAGERCONTROL','Disable')])
add('LaunchCondition',[('VersionNT64','QuietCare requires Windows x64. The application supports Windows 10/11.'),('NOT NEWERPRODUCTS','A newer QuietCare version is already installed. Use the newer installer.')])
add('Upgrade',[(upgrade,None,version,None,1,None,'OLDERPRODUCTS'),(upgrade,version,None,None,2,None,'NEWERPRODUCTS')])
view=db.OpenView("UPDATE InstallExecuteSequence SET Sequence=1510 WHERE Action='RemoveExistingProducts'");view.Execute(None);view.Close()
for table in ('InstallExecuteSequence','InstallUISequence'):
 view=db.OpenView("UPDATE "+table+" SET Sequence=50 WHERE Action='FindRelatedProducts'");view.Execute(None);view.Close()
add('Directory',[
 ('TARGETDIR',None,'SourceDir'),('LocalAppDataFolder','TARGETDIR','.'),('ProgramsDir','LocalAppDataFolder','Programs'),
 ('INSTALLDIR','ProgramsDir','QuietCareNext'),('ProgramMenuFolder','TARGETDIR','.'),('APP_MENU','ProgramMenuFolder','QuietCare'),('DesktopFolder','TARGETDIR','.'),('APP_DESKTOP','DesktopFolder','.')])
cab=msilib.CAB('QuietCarePayload.cab');file_ids={};directories={Path('.'):'INSTALLDIR'};manifest=[]
files=sorted(p for p in payload.rglob('*') if p.is_file())
for index,path in enumerate(files,1):
 relative=path.relative_to(payload);parent=relative.parent
 for directory in list(reversed(parent.parents))+[parent]:
  if directory==Path('.') or directory in directories:continue
  name='Dir'+hashlib.sha256(directory.as_posix().encode()).hexdigest()[:16];directories[directory]=name
  add('Directory',[(name,directories[directory.parent],directory.name)])
 file_id='File'+str(index);component='Cmp'+str(index);file_ids[relative.as_posix()]=file_id
 add('Component',[(component,guid('file/'+relative.as_posix()),directories[parent],256,None,file_id)])
 add('FeatureComponents',[('Main',component)])
 size=path.stat().st_size
 add('File',[(file_id,component,path.name,size,file_version if path.name=='QuietCare.Next.exe' else None,None,512,index)])
 cab.append(str(path),path.name,file_id);manifest.append({'path':relative.as_posix(),'bytes':size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
add('Feature',[('Main',None,'QuietCare','Windows PC-care application',1,1,'INSTALLDIR',0)])
registry_root=r'Software\QuietCareInstallerTest06' if args.fixture else r'Software\QuietCareNext\Install'
run_root=registry_root+r'\Run' if args.fixture else r'Software\Microsoft\Windows\CurrentVersion\Run'
approved_root=registry_root+r'\StartupApproved' if args.fixture else r'Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'
for name,condition in [('Links',None),('Desktop','DESKTOP="1"'),('Startup','STARTUP="1"')]:
 key='Reg'+name;cmp='Cmp'+name
 add('Component',[(cmp,guid(name),'INSTALLDIR',256,condition,None)])
 add('FeatureComponents',[('Main',cmp)])
 add('Registry',[(key,3,'[UserSID]\\'+registry_root,name,'[INSTALLDIR]',cmp)])
add('Registry',[
 ('RegRun',3,'[UserSID]\\'+run_root,'QuietCareNext','"[INSTALLDIR]QuietCare.Next.exe" --tray --monitor','CmpStartup'),
 ('RegApproved',3,'[UserSID]\\'+approved_root,'QuietCareNext','#x020000000000000000000000','CmpStartup')])
# Bind registration to the installing user's explicit hive. No service HKCU alias is used.
uninstall_key='[UserSID]\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\'+product
for index,(name,value) in enumerate([
 ('DisplayName','QuietCare Installer Fixture' if args.fixture else 'QuietCare'),('DisplayVersion',version+'-preview.1'),
 ('Publisher','QuietCare contributors'),('DisplayIcon','[INSTALLDIR]QuietCare.ico'),('InstallLocation','[INSTALLDIR]'),
 ('UninstallString','"[SystemFolder]msiexec.exe" /x '+product),('WindowsInstaller','#1'),('NoModify','#1'),('NoRepair','#1'),
 ('EstimatedSize','#'+str(sum(f['bytes'] for f in manifest)//1024)),('URLInfoAbout','https://github.com/EhosanurRahmanRomi/QuietCare')]):
 add('Registry',[('RegApp'+str(index),3,uninstall_key,name,value,'CmpLinks')])
icon=Path(__file__).resolve().parents[1]/'src/Cleaner.App/Assets/QuietCare.ico';add('Icon',[('QuietCareIcon',msilib.Binary(str(icon)))])
add('Shortcut',[
 ('StartMenu','APP_MENU','QuietCare','CmpLinks','[INSTALLDIR]QuietCare.Next.exe',None,'QuietCare PC care',None,'QuietCareIcon',0,1,'INSTALLDIR'),
 ('MonitorMenu','APP_MENU','QuietCare Performance Monitor','CmpLinks','[INSTALLDIR]QuietCare.Next.exe','--tray --monitor','Tiny CPU GPU RAM monitor',None,'QuietCareIcon',0,1,'INSTALLDIR'),
 ('DesktopLink','APP_DESKTOP','QuietCare','CmpDesktop','[INSTALLDIR]QuietCare.Next.exe',None,'QuietCare PC care',None,'QuietCareIcon',0,1,'INSTALLDIR')])
add('RemoveFile',[('RemoveAppMenu','CmpLinks',None,'APP_MENU',2)])
# Build a uniquely named cabinet to avoid the legacy library's temporary-name race.
cabpath=out.with_suffix('.payload.cab');msilib.FCICreate(str(cabpath),cab.files)
add('Media',[(1,cab.index,None,'#QuietCarePayload.cab',None,None)]);msilib.add_stream(db,'QuietCarePayload.cab',str(cabpath));db.Commit();cabpath.unlink()
si=db.GetSummaryInformation(20);si.SetProperty(msilib.PID_TEMPLATE,'x64;1033');si.SetProperty(msilib.PID_WORDCOUNT,10);si.SetProperty(msilib.PID_PAGECOUNT,500);si.SetProperty(msilib.PID_APPNAME,'QuietCare native Windows Installer builder');si.Persist();db.Commit();db=None
receipt={'productCode':product,'upgradeCode':upgrade,'version':version,'fixture':args.fixture,'startupDefault':True,'perUser':True,'files':manifest,'msiBytes':out.stat().st_size,'msiSha256':hashlib.sha256(out.read_bytes()).hexdigest()}
out.with_suffix('.manifest.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:receipt[k] for k in ['productCode','version','fixture','msiBytes','msiSha256']}))
