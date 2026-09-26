using UIKit;

namespace FieldReport.App;

/// <summary>Zezwala na gest „przesuń, by wrócić”, gdy jest dokąd wrócić (na stosie są co najmniej dwie strony).</summary>
public sealed class SwipeBackGesture(UINavigationController navigation) : UIGestureRecognizerDelegate
{
    public override bool ShouldBegin(UIGestureRecognizer recognizer) => navigation.ViewControllers?.Length > 1;
}
