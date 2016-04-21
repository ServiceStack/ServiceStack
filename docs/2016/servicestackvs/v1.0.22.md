## ServiceStackVS v1.0.22 Update
The latest update to ServiceStackVS includes various improvements to various templates, including:

- React Desktop Apps Packaged Installer using Squirrel.Windows.
- Integrated Auto-Update using GitHub ready to use.
- Move React Desktop Apps to TypeScript and JSPM.
- Updated React Desktop Apps and TypeScript React templates to React 15.0
- Simplified builds switching to Gulp for all templates.
- Simplify dependency management by deprecating use of Bower.
- Update AngularJS App template to Angular 1.5


[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/Squirrel-Logo.png)](https://github.com/Squirrel/Squirrel.Windows)

To improve the experience of deploying and installing of the standalone Windows application for the [React Desktop Apps template](https://github.com/ServiceStackApps/ReactChatApps), [Squirrel.Windows](https://github.com/Squirrel/Squirrel.Windows) has been incorporated so that you're application packages into a self executing installer that is setup to for quick automatic updates.

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/typescript-react-jspm-banner.png)](https://github.com/ServiceStackApps/typescript-react-template/)

This template has also been updated to use TypeScript, React, JSPM and Gulp to match what we think is the best way to produce web applications. The use of TypeScript with JSPM creates a smooth developer workflow with a great language from Microsoft that is improving all the time. TypeScript with Visual Studio 2015 gives you compile on save, intellisense and build errors all within Visual Studio itself.

#### Enabling Auto Updates using GitHub
To use GitHub for your releases of your Windows application updates, we need to have source committed to an accessible GitHub project. 

A few things should be added to the **default Visual Studio .gitignore** before we commit a new project. Ensure the following is in your .gitignore file.

```
node_modules/
jspm_packages/
wwwroot/
webdeploy.zip
```

Once you've created a project from the React Desktop Apps template, we need to change two pieces of config within the `App.config` in the **Host.AppWinForms** project, specifically `EnableAutoUpdate` to **true** and `UpdateManagerUrl` to your **GitHub project URL** (exclude the trailing slash).

``` xml
<configuration>
  ...
  <appSettings>
    <add key="EnableAutoUpdate" value="true" />
    <add key="UpdateManagerUrl" value="https://github.com/{Name}/{AppName}"/>
  </appSettings>
</configuration>
```

To package the Windows application we can use a preconfigured Gulp task called **02-package-winforms**. This will build all the required resources for your application and package them into a `Setup.exe` Windows installer. These files are located in the main project under **wwwroot_build\apps\winforms-installer**. The **Releases** folder contains all the distributables of your Windows application. 

```
MyReactApp
\_ wwwroot_build
  \_ apps
    \_ winforms-installer
      \_ Releases
        \_ MyReactApp-1.0.0.0-full.nupkg
        \_ RELEASES
        \_ Setup.exe 
```

To publish your initial version to GitHub, create a [Release in GitHub](https://help.github.com/articles/creating-releases/) and upload these 3 files in your releases folder.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/react-desktop-apps-release1.png)

Steps to update your application, eg to 1.1, would be the following.

- 1. Update the version of the AppWinForms project, either directly in `Properties/AssemblyInfo.cs` or through Project properties GUI.
- 2. Save changes and run the `02-package-winforms` Gulp task.
 
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/react-desktop-gulp-squirrel-package.png)


- 3. Commit your changes and push them to GitHub. (**This is required due to the new tag needs to be on a different commit**)
- 4. Create a new GitHub release and include the same 3 files, plus the **delta** NuGet package. Clients running `1.0.0.0` will detect the new version and updates can be easily managed with Squirrel.Windows.

>During step 2 your new version is picked up by the Gulp task and Squirrel creates a delta NuGet package, eg `MyReactApp-1.1.0.0-delta.nupkg` which will be used for quick updates to clients on the previous version (1.0). 

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/react-desktop-apps-release2.png)

Users that have installed version `1.0.0.0` will see a prompt already setup in the template that asks to update the application. By clicking update, the `delta` of `1.1.0.0` is downloaded and applied, then the application is restarted running the newer version of the application. 

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/auto-update-preview.gif)

#### Controlling Updates
Squirrel.Windows `UpdateManager` is the main class to control when and how your application gets updated. By default, the template hooks into a jQuery ready callback to fire the `NativeHost.Ready` method. This, in turn, checks for available updates and fires some JavaScript to notify the UI if an update is ready to download.

``` CSharp
// Notify web client updates are available.
formMain.InvokeOnUiThreadIfRequired(() =>
{
    formMain.ChromiumBrowser.GetMainFrame().ExecuteJavaScriptAsync("window.updateAvailable();");
});
```

Updating the UI for AppWinForms update is a platform specific customization so is handled independently in `platform.js` of the AppWinForms project. 

## Switching to Amazon S3 for releases
If you find the GitHub approach doesn't suit your needs, Squirrel.Windows also has support for Amazon S3 or any statically hosted URL. Though the template is setup to be used with GitHub, it can be easily changed to use Amazon S3. 

- Create an S3 bucket to host the `Releases` directory produced by the Gulp task  `02-package-winforms`. Eg,  `my-react-app`.

- Make your bucket publically read-only. To do this, you will need modify your "bucket policy" in S3. Below is the snippet to apply a public read policy, replace {BucketName} with the name of your bucket, eg `my-react-app`.
``` json
{
	"Version": "2008-10-17",
	"Statement": [
		{
			"Sid": "AllowPublicRead",
			"Effect": "Allow",
			"Principal": {
				"AWS": "*"
			},
			"Action": [
				"s3:GetObject"
			],
			"Resource": [
				"arn:aws:s3:::{BucketName}/*"
			]
		}
	]
}
```
- The URL configured in `UpdateManagerUrl` will need to be changed from the GitHub project URL to the S3 bucket URL, eg `https://s3-ap-southeast-2.amazonaws.com/my-react-app`.

- The `AppUpdater` static class in the AppWinForms project wraps Squirrel's `UpdateManager` to simplify correctly handling and disposing of the `UpdateManager`. To use Amazon S3, the static property `AppUpdateManager` can be changed from:

``` csharp
public static UpdateManager AppUpdateManager
{
    get
    {
        if (_updateManagerInstance != null)
        {
            return _updateManagerInstance;
        }

        var appSettings = new AppSettings();
        var updateManagerTask =
            UpdateManager.GitHubUpdateManager(appSettings.GetString("UpdateManagerUrl"));
        updateManagerTask.Wait(TimeSpan.FromMinutes(1));
        _updateManagerInstance = updateManagerTask.Result;
        return _updateManagerInstance;
    }
}
```

To:

``` csharp
public static UpdateManager AppUpdateManager
{
    get
    {
        if (_updateManagerInstance != null)
        {
            return _updateManagerInstance;
        }

        var appSettings = new AppSettings();
        _updateManagerInstance = new UpdateManager(appSettings.GetString("UpdateManagerUrl"));
        return _updateManagerInstance;
    }
}
```

Now that the client is ready to use with your new S3 bucket, you just need to upload the new files produced by Squirrel.Windows via the Gulp task `02-package-winforms` to your bucket. Clients will read the updates `RELEASE` file to detect any changes. If you are uploading your first release, it will be the following 3 files.
```
MyReactApp-1.0.0.0-full.nupkg
RELEASES
Setup.exe 
```

Subsequent releases will also include a `delta` file.

### Squirrel Installer Customization
Another customization Squirrel.Windows can do is control the icon used when your program is listed in the Windows Programs and Features UI. Since Squirrel uses NuGet heavily for packaging, these details come from what get put into the NuGet package that is produced by Gulp task `www-nuget-pack-winforms`. The template uses the [`gulp-nuget-pack`](https://www.npmjs.com/package/gulp-nuget-pack) module which can be customized to popuate the [different NuGet properties](https://docs.nuget.org/create/nuspec-reference), including the `iconUrl`.

Squirrel.Windows can also provide a loading GIF for your users when first installing your application. This can be provided by the `-g` command line argument. The call to the Squirrel.exe command line can be customized in the templated `gulpfile.js` in the `www-exec-package-winforms` task to apply this change.

#### Deprecating Grunt and Bower
Since the introduction of Single Page Application templates in ServiceStackVS, Grunt was used in conjunction with Gulp to be able to leverage packages from both eco systems. This however forced developers to learn the use of both build tools and how to integrate Gulp tasks into Grunt. 

Since then, Gulp has become the dominant tool in this space and it's concise syntax and performance makes it a better choice to script your JavaScript build environment.

Bower was also previously used to manage front-end dependencies separately from development related dependencies. Whilst this separation makes sense, NPM has moved to become the place where nearly all dependencies are now available on NPM which removes another moving part from smooth development workflow.
