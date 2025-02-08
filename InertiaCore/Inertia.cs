﻿using System.Runtime.CompilerServices;
using InertiaCore.Props;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Html;

[assembly: InternalsVisibleTo("InertiaCoreTests")]

namespace InertiaCore;

public static class Inertia
{
    private static IResponseFactory _factory = default!;

    internal static void UseFactory(IResponseFactory factory) => _factory = factory;

    public static Response Render(string component, object? props = null) => _factory.Render(component, props);

    public static Task<IHtmlContent> Head(dynamic model) => _factory.Head(model);

    public static Task<IHtmlContent> Html(dynamic model) => _factory.Html(model);

    public static void Version(object? version) => _factory.Version(version);

    public static string? GetVersion() => _factory.GetVersion();

    public static LocationResult Location(string url) => _factory.Location(url);

    public static void Share(string key, object? value) => _factory.Share(key, value);

    public static void Share(IDictionary<string, object?> data) => _factory.Share(data);

    public static AlwaysProp Always(object? value) => _factory.Always(value);

    public static AlwaysProp Always(Func<object?> callback) => _factory.Always(callback);

    public static AlwaysProp Always(Func<Task<object?>> callback) => _factory.Always(callback);

    public static DeferProp Defer(Func<object?> callback, string group = "default") => _factory.Defer(callback, group);

    public static DeferProp Defer(Func<Task<object?>> callback, string group = "default") => _factory.Defer(callback, group);

    public static LazyProp Lazy(Func<object?> callback) => _factory.Lazy(callback);

    public static LazyProp Lazy(Func<Task<object?>> callback) => _factory.Lazy(callback);

    public static OptionalProp Optional(Func<object?> callback) => _factory.Optional(callback);

    public static OptionalProp Optional(Func<Task<object?>> callback) => _factory.Optional(callback);

    public static MergeProp Merge(object? value) => _factory.Merge(value);

    public static MergeProp Merge(Func<object?> callback) => _factory.Merge(callback);

    public static MergeProp Merge(Func<Task<object?>> callback) => _factory.Merge(callback);
}
