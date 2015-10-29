# FlowTomator

The main goal of this project is to provide a quick and easy way to build small automation scripts with a graph based user experience.
You can build your own automation flow by connecting nodes representing basic actions.

FlowTomator features an engine alowing to run your flows in console mode, and a visual designer and debugger with edit and continue, step by step evaluation among other tools. 

<center>
    <img width="48" src="https://raw.githubusercontent.com/jbatonnet/flowtomator/master/Icon-256.png" />
</center>

## Structure

- **FlowTomator.Common** : The core project exposing basic nodes and flows runtime. This is the one you'll need to embed is you want to use FlowTomator core in your own software and tools.
- **FlowTomator.Engine** : A basic console application used to load a flow and evaluate it. This is typically the easiest way to run an existing flow.
- **FlowTomator.Desktop** : A desktop software to design and debug flows. Written in WPF, it will allow you to visually create and run your flows.
- **FlowTomator.Service** : A service used to administrate and run your flows when your computer boots. You will be able to detect and automate actions before you log in your session. This project provide a service monitor process.

## Plugins

FlowTomator features the core ability to load and use assembly plugins. Everyone should be able to easily write a plugin to connect or manipulate other APIs or softwares. Here are some of the plugins I personnaly use :

- **FlowTomator.SmartSync** : A simple FlowTomator plugin to manipulate and run [SmartSync](https://github.com/jbatonnet/smartsync) synchronization profiles. It can be used to automate your backups, deployments, ...

<center>
    <img width="48" src="https://raw.githubusercontent.com/jbatonnet/flowtomator/master/Icon-256.png" />
</center>

## Development

Even if the basic structure is working, the tools and the nodes need to be improved. There are still a lot of things to be done :

- Only some basic nodes are provided. We need to add a lot of new actions to manipulate data, files, text, web, devices, ...
- The debugger needs some polish. Some features are available in engine but not yet in the designer like variable creation and manipulation or plug-in import and usage.
- The service project is still an early draft. The monitor uses .NET Remoting functionalities to connect and administrate the service process.

## Background

These projects are using C# 6 programming. You can edit and build them using Visual Studio 2015.

I used WPF to build the designer to discover and learn the framework. I made some personal choices and tried some funky things to see how it can behave. I truly believe this is how we - developpers - can find awesome things. Don't blame me for that :)