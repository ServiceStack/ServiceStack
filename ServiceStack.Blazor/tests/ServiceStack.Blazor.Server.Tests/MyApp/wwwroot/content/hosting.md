---
title: Hosting Costs
WARN: During development Browser Cache needs to be disabled to refresh .md changes
---

# App Hosting Costs

<a href="https://jamstack.org">
    <img src="/img/jamstack-icon.svg" style="width:3.5rem;height:3.5rem;float:left;margin:.5rem 1rem 0 0">
</a>

The modern [jamstack.org](https://jamstack.org) approach for developing websites is primarily concerned with adopting 
the architecture yielding the best performance and superior UX by minimizing the time to first byte from serving 
pre-built static assets from CDN edge caches.

## Cheaper Hosting

<a href="https://jamstack.org">
    <img src="/img/emoji-money.svg" style="width:3.5rem;height:3.5rem;float:left;margin:.5rem 1rem 0 0">
</a>

A consequence of designing your UI decoupled from your back-end server is that it also becomes considerably 
cheaper to host as its static files can be hosted by any web server and is a task highly optimized by CDNs
who are able to provide generous free & low cost hosting options.

##  [/MyApp.Client](https://github.com/NetCoreTemplates/blazor-tailwind/tree/main/MyApp.Client)

This template takes advantage of its decoupled architecture and uses [GitHub Actions to deploy](/docs/deploy) 
a copy of its static UI generated assets and hosted on:

### GitHub Pages CDN

### [blazor-wasm.jamstacks.net](https://blazor-wasm.jamstacks.net)

This is an optional deployment step which publishes a copy of your .NET App's `/wwwroot` folder to this templates 
[gh-pages](https://github.com/NetCoreTemplates/blazor-tailwind/tree/gh-pages) branch where it's automatically served from 
[GitHub Pages CDN](https://docs.github.com/en/pages/getting-started-with-github-pages/about-github-pages) at **no cost**.

It's an optional but recommended optimization as it allows the initial download from your website to be served
directly from CDN edge caches.

## [/MyApp](https://github.com/NetCoreTemplates/blazor-tailwind/tree/main/MyApp)

The .NET 6 `/MyApp` backend server is required for this App's dynamic functions including the Hello API on the home page
and its [built-in Authentication](https://docs.servicestack.net/auth). 

The C# project still contains the complete App and can be hosted independently with the entire App served 
directly from its deployed ASP.NET Core server at:

### Digital Ocean

### [blazor-wasm-api.jamstacks.net](https://blazor-wasm-api.jamstacks.net)

But when accessed from the CDN [blazor-wasm.jamstacks.net](https://blazor-wasm.jamstacks.net) that contains a 
copy of its static `/wwwroot` UI assets, only its back-end JSON APIs are used to power its dynamic features.

## Total Cost

<a href="https://www.digitalocean.com/pricing">
    <img src="/img/digital-ocean.svg" style="width:6.5rem;height:6.5rem;float:left;margin:0 1rem 0 0">
</a>

Since hosting on GitHub Pages CDN is free, the only cost is for hosting this App's .NET Server which is being hosted 
from a basic [$10 /mo](https://www.digitalocean.com/pricing) droplet which is currently hosting **25** .NET Docker 
Apps and demos of [starting project templates](https://servicestack.net/start) which works out to be just under **$0.40 /mo**!

## Jamstack Benefits

Jamstack is quickly becoming the preferred architecture for the development of modern web apps with 
[benefits](https://jamstack.org/why-jamstack/) that extend beyond performance to improved: 

 - **Security** from a reduced attack surface from hosting read-only static resources and requiring fewer App Servers
 - **Scale** with non-essential load removed from App Servers to CDN's architecture capable of incredible scale & load capacity
 - **Maintainability** resulting from reduced hosting complexity and the clean decoupling of UI and server logic
 - **Portability** with your static UI assets being easily capable from being deployed and generically hosted from any CDN or web server
 - **Developer Experience** with the major JavaScript frameworks at the forefront of amazing DX are embracing Jamstack in their dev model, libraries & tooling  

Best of all the Jamstack approach fits perfectly with ServiceStack's recommended 
[API First Development](https://docs.servicestack.net/api-first-development) model which encourages development of
reusable message-based APIs where the same System APIs can be reused from all Web, Mobile & Desktop Apps 
from multiple HTTP, MQ or gRPC endpoints.
