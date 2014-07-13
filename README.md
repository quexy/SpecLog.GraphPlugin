# SpecLog Graph Plugin
*Repository Activity Graph for SpecLog*


### About
This plugin was created in early 2014, when SpecLog 1.13's features were implemented, as a proof of concept to use SpecLog as a tool to help Project Owners, Scrum Masters, Project Managers and whatever you call them to collect various benchmarks of the work based on the activity of the SpecLog project repository.

The plugin &ndash; as usually SpecLog plugins do &ndash; comprises from a client (configuration interface) and a server (actual behaviour) part. The installer deploys only that half of the plugin which the current SpecLog installation requires, that is, if the server part of SpecLog was installer, then the server side of the plugin will not be deployed.


### Graphs
Currently the plugin makes two types of graph available, a 30-day change frequency graph and a lifetime change GitHub-style punchcard graph. These graphs are available directly from the speclog server as simple HTML pages, not unlike the repository HTML export. The plugin supports a single user basic authentication to restrict access to the generated graphs.


### Configuration
From the SpecLog client menu select the `Manage repositories` menu item, connect to your server.
> !['Manage repositories' menu option](https://raw.githubusercontent.com/wiki/quexy/SpecLog.GraphPlugin/images/manage-repo-menu.png)


Pick a project for which you want to configure the plugin from the list, then click on the `Configure plugins` option.
> !['Configure plugins' for a repository](https://raw.githubusercontent.com/wiki/quexy/SpecLog.GraphPlugin/images/plugin-conf-project-screen.png)

In the plugin list choose `Graph Plugin` and select `Configure` from the plugin menu.
> !['Configure' graph plugin](https://raw.githubusercontent.com/wiki/quexy/SpecLog.GraphPlugin/images/plugin-conf-select-screen.png)

On the appearing dialogue you can turn on the plugin (`Enable plugin`) and set up access restriction (`Viewer credentials`) if you so desire, apart from obtaining a URL (`Publish url`; it contains the identifier of the repository) for accessing the generated graphs. Please note, that the url is live while the plugin is turned on.
> ![Plugin configuration dialogue](https://raw.githubusercontent.com/wiki/quexy/SpecLog.GraphPlugin/images/plugin-conf-screen.png)


### Notes
To keep historical data, you either need to run the SpecLog upgrade tool with the `-keeepHistory` switch or back up and restore the plugin repository (usually from `%ProgramData%\TechTalk\SpecLog\Plugins`).
 
You should consider using HTTPS if you turn on access restriction. But then again, you should consider turning on HTTPS regardless. Even so if your SpecLog server is open to the internet. It is safer that way.
