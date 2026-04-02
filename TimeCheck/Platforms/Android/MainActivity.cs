using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TimeCheck;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		try
		{
			var root = Window?.DecorView?.FindViewById(Android.Resource.Id.Content) as Android.Views.ViewGroup;
			if (root != null)
			{
				// Try to resolve the expected container id 'jumpToStart' from resources
				int containerId = Resources?.GetIdentifier("jumpToStart", "id", PackageName) ?? 0;
				if (containerId == 0)
				{
					// If resource id isn't present at runtime, generate a view id so fragment lookup won't fail with null reference
					containerId = Android.Views.View.GenerateViewId();
				}

				var existing = root.FindViewById(containerId);
				if (existing == null)
				{
					var fl = new Android.Widget.FrameLayout(this) { Id = containerId };
					root.AddView(fl, new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent));
				}
			}
		}
		catch (System.Exception)
		{
			// Don't let this helper crash the startup path; it's only a best-effort mitigation for fragment container issues.
		}
	}
}
