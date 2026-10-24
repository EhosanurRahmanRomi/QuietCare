using System.Windows;
using System.Windows.Media.Animation;
namespace Cleaner.App;

// A short, one-shot page fade. No timer, continuous decoration or monitor animation.
public static class CareMotion
{
    public static readonly DependencyProperty EnterOnLoadProperty=DependencyProperty.RegisterAttached("EnterOnLoad",typeof(bool),typeof(CareMotion),new PropertyMetadata(false,Attach));
    public static bool GetEnterOnLoad(DependencyObject value)=>(bool)value.GetValue(EnterOnLoadProperty);
    public static void SetEnterOnLoad(DependencyObject value,bool enabled)=>value.SetValue(EnterOnLoadProperty,enabled);
    static void Attach(DependencyObject value,DependencyPropertyChangedEventArgs args)
    {
        if(value is not FrameworkElement element)return;
        element.Loaded-=Loaded;if((bool)args.NewValue)element.Loaded+=Loaded;
    }
    static void Loaded(object sender,RoutedEventArgs args)=>Enter((FrameworkElement)sender);
    public static bool Enabled {get;set;}=true;
    public static bool Allowed=>Enabled&&SystemParameters.ClientAreaAnimation&&!SystemParameters.HighContrast;
    public static int TransitionsStarted {get;private set;}
    public static void Enter(FrameworkElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty,null);
        if(!Allowed)return;
        var fade=new DoubleAnimation(.78,1,TimeSpan.FromMilliseconds(180))
        {EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};
        TransitionsStarted++;element.BeginAnimation(UIElement.OpacityProperty,fade,HandoffBehavior.SnapshotAndReplace);
    }
}
