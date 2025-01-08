# Inertia.js ASP.NET Adapter

[![NuGet](https://img.shields.io/nuget/v/AspNetCore.InertiaCore?style=flat-square&color=blue)](https://www.nuget.org/packages/AspNetCore.InertiaCore)
[![Build](https://img.shields.io/github/actions/workflow/status/kapi2289/InertiaCore/dotnet.yml?style=flat-square)](https://github.com/kapi2289/InertiaCore/actions)
[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.InertiaCore?style=flat-square)](https://www.nuget.org/packages/AspNetCore.InertiaCore)
[![License](https://img.shields.io/github/license/kapi2289/InertiaCore?style=flat-square)](https://github.com/kapi2289/InertiaCore/blob/main/LICENSE)

## Features

- [x] Validation error handling.
- [x] Shared data.
- [x] Partial and async lazy props.
- [x] Server-side rendering.
- [x] Vite helper.
- [x] Cycle-safe model with relations data serialization.
- [x] Properly working **PATCH**, **PUT** and **DELETE** redirections.

## Table of contents

- [Examples](#examples)
- [Installation](#installation)
- [Getting started](#getting-started)
- [Usage](#usage)
  * [Frontend](#frontend)
  * [Backend](#backend)
- [Features](#features)
  * [Shared data](#shared-data)
  * [Async Lazy Props](#async-lazy-props)
  * [Server-side rendering](#server-side-rendering)
  * [Vite helper](#vite-helper)
    - [Examples](#examples-1)

## Examples

You can check out these examples to have some starting point for your new application.

- **Vue** - [NejcBW/InertiaCoreVueTemplate](https://github.com/NejcBW/InertiaCoreVueTemplate)
- **React** - [nicksoftware/React-AspnetCore-inertiaJS](https://github.com/nicksoftware/React-AspnetCore-inertiaJS)

## Installation

1. Using Package Manager: `PM> Install-Package AspNetCore.InertiaCore`
2. Using .NET CLI: `dotnet add package AspNetCore.InertiaCore`

## Getting started

You need to add few lines to the `Program.cs` or `Starup.cs` file.

```csharp
using InertiaCore.Extensions;

[...]

builder.Services.AddInertia();

[...]

app.UseInertia();
```

## Usage

### Frontend

Create a file `/Views/App.cshtml`.

```html
@using InertiaCore
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title inertia>My App</title>
</head>
<body>
@await Inertia.Html(Model)

<script src="/js/app.js"></script>
</body>
</html>
```

You can change the root view file using:

```csharp
builder.Services.AddInertia(options =>
{
    options.RootView = "~/Views/Main.cshtml";
});
```

### Backend

To pass data to a page component, use `Inertia.Render()`.

```csharp
    public async Task<IActionResult> Index()
    {
        var posts = await _context.Posts.ToListAsync();
        
        var data = new
        {
            Posts = posts,
        };
        
        return Inertia.Render("Posts", data);
    }
```

To make a form endpoint, remember to add `[FromBody]` to your model parameter, because the request data is passed using
JSON.

```csharp
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Post post)
    {
        if (!ModelState.IsValid)
        {
            // The validation errors are passed automatically.
            return await Index();
        }
        
        _context.Add(post);
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Index");
    }
```

## Features

### Shared data

You can add some shared data to your views using for example middlewares:

```csharp
using InertiaCore;

[...]

app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("userId");
    
    Inertia.Share("auth", new
    {
        UserId = userId
    });
    
    // Or
    
    Inertia.Share(new Dictionary<string, object?>
    {
        ["auth"] => new
        {
            UserId = userId
        }
    });
});
```

### Async Lazy Props

You can use async lazy props to load data asynchronously in your components. This is useful for loading data that is not needed for the initial render of the page.
```csharp

// simply use the LazyProps the same way you normally would, except pass in an async function

    public async Task<IActionResult> Index()
    {
        var posts = new LazyProp(async () => await _context.Posts.ToListAsync());
        
        var data = new
        {
            Posts = posts,
        };
        
        return Inertia.Render("Posts", data);
    }


```

### Server-side rendering

If you want to enable SSR in your Inertia app, remember to add `Inertia.Head()` to your layout:

```html
@using InertiaCore
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title inertia>My App</title>
    
    @await Inertia.Head(Model)
</head>
<body>
@await Inertia.Html(Model)

<script src="/js/app.js"></script>
</body>
</html>
```

and enable the SSR option:

```csharp
builder.Services.AddInertia(options =>
{
    options.SsrEnabled = true;
    
    // You can optionally set a different URL than the default.
    options.SsrUrl = "http://127.0.0.1:13714/render"; // default
});
```

### Vite Helper

A Vite helper class is available to automatically load your generated styles or scripts by simply using the `@Vite.Input("src/main.tsx")` helper. You can also enable HMR when using React by using the `@Vite.ReactRefresh()` helper. This pairs well with the `laravel-vite-plugin` npm package.

To get started with the Vite Helper, you will need to add one more line to the `Program.cs` or `Starup.cs` file.

```csharp
using InertiaCore.Extensions;

[...]

builder.Services.AddViteHelper();

// Or with options (default values shown)

builder.Services.AddViteHelper(options =>
{
    options.PublicDirectory = "wwwroot";
    options.BuildDirectory = "build";
    options.HotFile = "hot";
    options.ManifestFilename = "manifest.json";
});
```


#### Examples
---

Here's an example for a TypeScript React app with HMR:

```html
@using InertiaCore
@using InertiaCore.Utils
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title inertia>My App</title>
  </head>
  <body>
    @* This has to go first, otherwise preamble error *@
    @Vite.ReactRefresh()
    @await Inertia.Html(Model)
    @Vite.Input("src/main.tsx")
  </body>
</html>
```

with the corresponding `vite.config.js`, which is recommended to create in the root solution directory:

```js
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import laravel from "laravel-vite-plugin";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    laravel({
      input: ["src/main.tsx"],
      publicDirectory: "wwwroot/",
    }),
    react(),
  ],
});
```

---

Here's an example for a TypeScript Vue app with Hot Reload:

```html
@using InertiaCore
@using InertiaCore.Utils
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title inertia>My App</title>
  </head>
  <body>
    @await Inertia.Html(Model)
    @Vite.Input("src/app.ts")
  </body>
</html>
```

with the corresponding `vite.config.js`, which is recommended to create in the root solution directory:

```js
import {defineConfig} from 'vite';
import vue from '@vitejs/plugin-vue';
import laravel from "laravel-vite-plugin";

export default defineConfig({
  plugins: [
    laravel({
      input: ["src/app.ts"],
      publicDirectory: "wwwroot/",
      refresh: true,
    }),
    vue({
      template: {
        transformAssetUrls: {
          base: null,
          includeAbsolute: false,
        },
      },
    }),
  ],
});
```

---

Here's an example that just produces a single CSS file:

```html
@using InertiaCore
@using InertiaCore.Utils
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  </head>
  <body>
    @await Inertia.Html(Model)
    @Vite.Input("src/main.scss")
  </body>
</html>
```
