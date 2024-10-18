# Headless CMS

This project currently is under heavy development. It consists of two parts - standalone content management system, and some SDK for content consuming applications. The SDK is the abstraction over the database, querying it directly. The third project **Test** is just a playground for debugging purposes, not unit tests.

Decomposition of documents visual content into simple structured fragments is in the main focus of development. Once decomposition of some visual fragment (like card, hero section, accordion, grid with rows and cols, etc) is defined and described, CMS can display its html form and save entered values to the database. To describe the decomposition, XML schemata are used.

For example, the [card](https://getbootstrap.com/docs/5.3/components/card/) in simpliest case generally consists of title, image, text, and one or more "call-to-action". Its basic decomposition description will be:

<details>
  <summary>Click to expand</summary>

```
<?xml version="1.0" encoding="utf-8"?>

<xs:schema
	targetNamespace="http://h-cms.net/cms/new-schema.xsd"
	elementFormDefault="qualified"
	xmlns="http://h-cms.net/cms/new-schema.xsd"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:xsc="http://aleprojects.com/custom">

	<xs:complexType name="simple-card">
		<xs:annotation>
			<xs:documentation xml:lang="en">Simple Card</xs:documentation>
			<xs:documentation xml:lang="fr">Carte simple</xs:documentation>
		</xs:annotation>
		<xs:sequence>
			<xs:element name="title" type="xs:string">
				<xs:annotation>
					<xs:documentation xml:lang="en">Title</xs:documentation>
					<xs:documentation xml:lang="fr">Titre</xs:documentation>
				</xs:annotation>
			</xs:element>
			<xs:element name="text" type="xs:string">
				<xs:annotation>
					<xs:documentation xml:lang="en">Text</xs:documentation>
					<xs:documentation xml:lang="fr">Texte</xs:documentation>
					<xs:appinfo>
						<xsc:properties textformat="html"></xsc:properties>
					</xs:appinfo>
				</xs:annotation>
			</xs:element>
			<xs:element name="layout">
				<xs:annotation>
					<xs:documentation xml:lang="en">Layout</xs:documentation>
					<xs:documentation xml:lang="fr">Disposition</xs:documentation>
				</xs:annotation>
				<xs:simpleType>
					<xs:restriction base="xs:token">
						<xs:enumeration value="start"/>
						<xs:enumeration value="end"/>
						<xs:enumeration value="over"/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
			<xs:element name="image" type="xs:anyURI">
				<xs:annotation>
					<xs:documentation xml:lang="en">Link</xs:documentation>
					<xs:documentation xml:lang="fr">Lien</xs:documentation>
				</xs:annotation>
			</xs:element>
			<xs:element name="link" type="xs:anyURI" maxOccurs="3">
				<xs:annotation>
					<xs:documentation xml:lang="en">Image</xs:documentation>
					<xs:documentation xml:lang="fr">Image</xs:documentation>
				</xs:annotation>
			</xs:element>
		</xs:sequence>
	</xs:complexType>

    <xs:element name="simple-card" type="simple-card"/>
</xs:schema>
```

`<xs:documentation>` values are used as form field labels for different UI languages.

</details>

Rendered form will look

<details>
  <summary>Click to expand</summary>

  ![form](https://h-cms.net/simple-card-form.png)

</details>

Note: all this don't require project rebuild and redeploy.

## Prerequisites

Below are the necessary steps to take in order to run it in Visual Studio 2022.

### Database connection

CMS uses MS SQL database. Provide the connection string in the `settings.json` file. 

### External authorization

CMS uses external authorization. Google, Microsoft, and Github are currently supported. It is easier to create Github OAuth app for the first time. Click you profile icon at the top-right, select `Settings`, select `Developer settings` (at the very bottom), then `OAuth App`, and finally click `New OAuth App`.

In appeared form set these fields as

`Application name`: "Cms" or whatever,  
`Homepage Url`: https://localhost:7284  
`Authorization callback URL`: https://localhost:7284/api/v1/auth/github

Save generated ClientId and Client Secret. Specify them [in settings.json file](https://github.com/LeoAlexandrov/Cms/blob/master/AleProjects.Cms/settings.json#L35)  
Note1: Client Secret will be displayed only once right after creation of the OAuth app.  
Note2: Another Github OAuth app will be required for production. Microsoft and Google allow multiple Url/callbacks for a single application.

### App configuration

`appsettings.[Development].json` files link external settings file `settings.json`.  
Fix [this line](https://github.com/LeoAlexandrov/Cms/blob/master/AleProjects.Cms/appsettings.Development.json#L10) to **"settings.json"** value to use file from the repository. Or copy linked files to some outer folder.

## Demo

Here [admin.h-cms.net](https://admin.h-cms.net) is the current demo version with guest access. But guest users have readonly access rights.

_To be continued..._