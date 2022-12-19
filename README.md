# Inertia.js ASP.NET Adapter

[![NuGet](https://img.shields.io/nuget/v/AspNetCore.InertiaCore?style=flat-square)](https://www.nuget.org/packages/AspNetCore.InertiaCore)
[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.InertiaCore?style=flat-square)](https://www.nuget.org/packages/AspNetCore.InertiaCore)
[![License](https://img.shields.io/github/license/kapi2289/InertiaCore?style=flat-square)](https://github.com/kapi2289/InertiaCore/blob/main/LICENSE)

## Attribution

This library is heavily inspired
by [Nothing-Works/inertia-aspnetcore](https://github.com/Nothing-Works/inertia-aspnetcore), but it has some errors fixed
and its usage is more similar to the official adapters'.

## What was added

- [x] Cycle-safe model relations data serialization.
- [x] Validation error handling.
- [x] Better shared data integration.
- [x] Props and shared props are merged instead of being separated.
- [x] Fixed **PATCH**, **PUT**, **DELETE** redirection not working properly.

## Installation

1. Using Package Manager: `PM> Install-Package AspNetCore.InertiaCore`
2. Using .NET CLI: `dotnet add package AspNetCore.InertiaCore`

## Getting started

You need to add few lines to the `Program.cs` or `Starup.cs` file.

```csharp
using InertiaCore.Extensions;

[...]

builder.Services.AddControllersWithViews()
    .AddInertiaOptions(); // Configure MVC options
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
@Inertia.Html(Model)

<script src="/js/app.js"></script>
</body>
</html>
```

You can change the root view file using

```csharp
app.UseInertia();
Inertia.SetRootView("~/Views/Main.cshtml");
```

in your `Program.cs` or `Startup.cs` file.

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

You can add some shared data to your views using for example middlewares.

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
