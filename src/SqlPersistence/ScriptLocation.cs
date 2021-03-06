﻿using System;
using System.IO;
using System.Reflection;
using NServiceBus;

static class ScriptLocation
{
    public static string FindScriptDirectory(SqlDialect dialect)
    {
        var currentDirectory = GetCurrentDirectory();
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", dialect.Name);
    }

    static string GetCurrentDirectory()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        var codeBase = entryAssembly.CodeBase;
        return Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint or NServiceBus.Persistence.Sql.MsBuild nuget was not included in the project.");
        }
    }
}