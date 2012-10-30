# BugSense for Windows Phone 8

![Mou icon](http://www.panicnot.com/image/bugsense-min-in.png)

### Installing the library

Install the BugSense [NuGet package](http://nuget.org/packages/BugSense.WP8) using the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console).

	PM> Install-Package BugSense.WP8

You can also use the [Package Management Dialog](http://docs.nuget.org/docs/start-here/managing-nuget-packages-using-the-dialog) in Visual Studio to install the BugSense.WP8 package. 

![Package Manager](http://www.bugsense.com/static/images/landing/screens/install.jpg)

#### Initialize the plugin ###

Then, all you need to do is go inside you **App.xaml.cs** file and add the code below inside the constructor. Don't forget to use your Project API key that you'll find it in your dashboard!

***Note***: If you are in debugging mode (ex. Step-by-Step) BugSense Handler will recognize that and will not send the exception. This happens because when you are debugging your app, Visual Studio does a great job on letting you know where the error is.

```c#
public App()
{
     // Standard XAML initialization
     InitializeComponent();

     // Phone-specific initialization
     InitializePhoneApplication();

     // Language display initialization
     InitializeLanguage();

     // Initialize BugSense
     BugSenseHandler.Instance.Init(this, "YOUR_API_KEY");
}
```
![App.xaml.cs](http://localhost:8080/static/images/landing/screens/linecode.jpg)

You can also remove any Unhandled Exception handlers like this one:
	
	UnhandledException += Application_UnhandledException;

or you can replace with the following

	BugSenseHandler.Instance.UnhandledException += OnUnhandledException;

Now you can ship your application and stay cool. We will make sure 
you won't miss a bug.

### End User Communication

When an error occurs the user is informed by a slick Popup. This option is customizable. You can:

* Display a [Fix Notification](http://www.bugsense.com/features/notifications) when a user experiences an error that is been fixed in new versions of the app
* Display nothing and just keep the application running.
* Display a popup with customizable Title and Body.
* Display a confirmation popup with customizable Title and Body allowing the user to decide if he wants to send the error report. An example screenshot is displayed below: 

![Fix Notification](http://www.bugsense.com/static/images/landing/screens/notification.jpg)

### Advanced Settings 

You can also use BugSense to log handled exceptions and send useful Metadata
```c#
try
{
    throw new IndexOutOfRangeException();
}
catch (Exception exc)
{
    BugSenseHandler.AddToLogData("account", "Explorer");
    BugSenseHandler.AddToLogData("level", "9");
    BugSenseHandler.Instance.LogError(exc);
}
```

Monitor if async Tasks have been completed successfully
```c#
// Check is a task if task is faulted and log task exception if it failed

Task t = new Task(() =>
{
   int x = 5;
   int y = 0;
   int z = x / y;
});

t.Start();
BugSenseHandler.CheckTaskFault("mathtask", t);
```

You can even override the user notification options
```c#
try {
    throw new InvalidOperationException("An exception occured while executing in a different thread.");
}
catch (Exception ex) {
    var overridenOptions = BugSenseHandler.DefaultOptions();
    overridenOptions.Text = ex.Message + Environment.NewLine + "Do you want to send the Exception?";
    overridenOptions.Type = enNotificationType.MessageBoxConfirm;
    BugSenseHandler.HandleError(ex, string.Format("User {0}", _user), overridenOptions);
}
```

Add Breadcrumbs and meta data to crash reports
```c#
// Adding custom extra to be sent with the crash

BugSenseHandler.AddToExtraData("xtra1key", "xtra1val");
BugSenseHandler.AddToExtraData("xtra2key", "xtra2val");

// Leave a breadcrump

BugSenseHandler.LeaveBreadcrumb("Fetch friends");
```

[On-line documentation browser](http://bit.ly/bugsense-wp8-docs)
