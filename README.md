# H-Cms.Net

This project is currently under heavy development. It consists of two main parts: a standalone content management system (HCms.Web) and NuGet packages (HCms, HCms.Content.ViewModels) for content-consuming applications.

There are also the **HCms.Domain**, **HCms.Infrastructure**, **HCms.Infrastructure.Data**, and **HCms.Application** projects in the solution which are shared core modules managing documents, visual fragments, media library, etc.

The goal of the project is to enable users to create rich page content without any knowledge of HTML/CSS and, at the same time, to provide maximum flexibility for backend developers.

The CMS operates with documents that can typically be thought of as a 'web page' and with media files. Documents are organized into a tree-like hierarchy reflecting the 'site map', the CMS supports multiple independent trees (sites). Each document has a fixed set of common properties (like title, slug, meta tags, language, etc.) and visual content decomposed into fragments (content blocks). Fragments represent such visual elements as cards, hero sections, accordions, grids with rows and columns, etc. Fragments like grids or accordions serve as containers for other fragments, allowing creation of complex layouts.

A key requirement for the decomposition is that fragments should be represented as objects with well-defined properties containing only data (such as 'title', 'subtitle', 'number of columns', 'alignment', etc) free from any specific HTML/CSS styling. This allows the CMS to render HTML forms for content editors to fill out and also allows developers of content consuming apps to make flexible partial views for fragments rendering.

XML schemas are used to define the structure of fragments, using them the CMS generates HTML forms and validates input. [Here](https://github.com/LeoAlexandrov/Cms/tree/master/HCms.Web/InitialData/XmlSchemata) are the default predefined schemata provided with the CMS. Developers can create their own or extend existing schemata to define custom fragments. This does not require rebuilding or redeploying the CMS.

Media content can be stored in local filesystem or in S3 buckets.

#### Demos

[admin.h-cms.net](https://admin.h-cms.net) - The control panel with guest access. You can sign in anonymously, without revealing your e-mail or account login, using the 'Sign in as anonymous guest' button, simply passing the Cloudflare check.

[demo.h-cms.net](https://demo.h-cms.net) - The demo site consuming CMS content; see [this project](https://github.com/LeoAlexandrov/Cms-demos/tree/master/DemoSite).

## Prerequisites

Below are the necessary steps to take in order to run this project in Visual Studio.

#### Database connection

The CMS can use MS SQL Server (default), PostgreSQL, or MySQL database engines. You must provide the connection string in the [settings.json](https://github.com/LeoAlexandrov/Cms/blob/master/HCms.Web/settings.json) file.
<details>
  <summary>Click to expand</summary>

```json
{
	"DbEngine": "mssql", // "postgres" or "mysql"

	"ConnectionStrings": {
		"CmsDbConnection": "data source=<your-mssql-host>;initial catalog=Cms;TrustServerCertificate=true;persist security info=True;user id=user;password=uSer_Pa$$w0rd;App=EntityFramework"
		//"CmsDbConnection": "Host=<your-pgsql-host>;Port=5432;Database=Cms;Username=user;Password=uSer_Pa$$w0rd"
		//"CmsDbConnection": "server=<your-mysql-host>;database=Cms;user=user;password=uSer_Pa$$w0rd"
	},
...
}
```
</details>

#### External authorization

Presently, CMS uses only external authorization. Google, Microsoft, Github, and StackOverflow are currently supported. It is easier to create a Github OAuth app for the first time. Click your Github profile icon at the top-right, select `Settings`, select `Developer settings` (at the very bottom), then `OAuth App`, and finally click `New OAuth App`.

In the form that appears, set the following fields:

`Application name`: "Cms local" (or any other name you prefer),  
`Homepage Url`: https://localhost:7284  
`Authorization callback URL`: https://localhost:7284/api/v1/auth/github

Save the generated ClientId and Client Secret. Specify them in the [settings.json](https://github.com/LeoAlexandrov/Cms/blob/master/HCms.Web/settings.json#L46) file.
<details>
  <summary>Click to expand</summary>

```json
{
...
	"Auth": {
	...
		"Github": {
			"ClientId": "your-obtained-clientId",
			"ClientSecret": "your-obtained-clientSecret"
		},
	...
	}
...
}
```
</details>

**Note1**: The Client Secret will be displayed only once, immediately after the OAuth app is created.  
**Note2**: Another GitHub OAuth app will be required for production. GitHub, unlike Microsoft or Google, allows only one callback URL per application.  
**Note3**: The presense of the `Google`, `Microsoft`, `Github`, `StackOverflow`, `CloudflareTT` sections defines the visibility of the corresponding 'SIGN IN WITH ...' button on the login page.

Change ["SecurityKey"](https://github.com/LeoAlexandrov/Cms/blob/master/HCms.Web/settings.json#L9) value to something more secure. It must be 32 characters long. Leave other `Auth` settings as they are for the first time.

#### Media storage configuration

The CMS works with 'storage places.' Think of these as top-level root folders. Each place can be either a local folder (filesystem) or an S3 bucket. Places of different types can be used simultaneously. Both place types share a common, manually assigned property: the `Key`. When you reference an image in a document, the link CMS generates consists of the key + relative media file path. You can move a storage folder to another location or even switch its type to S3 (and vice versa); links won't break as long as you preserve the key.  
["Media" section](https://github.com/LeoAlexandrov/Cms/blob/master/HCms.Web/settings.json#L64) is responsible for media content storage configuration.
<details>
  <summary>Click to expand</summary>

```json
{
...
	"Media": {
		"CacheFolder": "\\var\\tmp\\hcms-media-cache",
		"MaxUploadSize": 10485760,
		"SafeNameRegex": "^[\\w-]+.\\w+$",
		"Buckets": [
			{
				"Endpoint": "https://s3.eu-west-1.amazonaws.com",
				"Name": "beneficial-aurelia",
				"AccessKey": "AKIAIOSFODNN7EXAMPLE",
				"SecretKey": "wJalrXUtnFEMIaK7MDENGbbPxRfiCYEXAMPLEKEY",
				"Key": "MySite"
			}
		],
		"LocalDiskPlaces": [
			{
				"Path": "\\var\\www\\hcms-demosite-media",
				"Key": "H-Cms demo"
			}
		]
	},
...
}
```
</details>

`CacheFolder` - full path of the folder where CMS caches preview thumbnails of the media files.  
`MaxUploadSize` - maximum allowed size of uploaded media files in bytes (10 MB by default). Don't forget to specify proper value in the nginx production configuration.  
`SafeNameRegex` - regular expression defining allowed characters in media file names. Applied to the CMS users with the privileges less than the 'Admin' role gives.  
`Buckets` - array with S3 bucket parameters.  
`LocalDiskPlaces` - array with local filesystem folder parameters.

##### S3 bucket parameters

`Endpoint` - service Url.  
`Name` - bucket name.  
`AccessKey` and `SecretKey` - bucket credentials.  
`Key` - storage place key.  

##### Local folder parameters

`Path` - full path of the folder.  
`Key` - storage place key.

**Note**: if you decide to deploy CMS as docker container and use filesystem media storage, mount local folders to the container.

#### All other settings

Can be left as they are for the first time.

## Starting the CMS

At its first run, the CMS will create database tables and seed [initial data](https://github.com/LeoAlexandrov/Cms/tree/master/HCms.Web/InitialData).
Then it will prompt to specify login of the first user with the 'Developer' role giving top privileges. Specify your Github login (or any other login depending on the configured external authorization providers).

Finally, it will redirect you to the login page where you can sign in with the specified account.

## Path mapping, documents linking, and URL generation

## Production deployment

_To be continued..._