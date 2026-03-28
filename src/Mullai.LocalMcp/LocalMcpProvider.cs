using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mullai.Abstractions.Configuration;

namespace Mullai.LocalMcp;

public class LocalMcpProvider : IBuiltInMcpProvider

{
    private readonly ILogger<LocalMcpProvider> _logger;
    private readonly string _toolKitsPath;
    private List<McpServerDescriptor>? _cachedServers;

    public LocalMcpProvider(ILogger<LocalMcpProvider> logger)
    {
        _logger = logger;
        // Use a relative path from the application base directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _toolKitsPath = Path.Combine(baseDir, "McpToolkits");
        
        _logger.LogInformation("Scanning for local MCP toolkits in {Path}", _toolKitsPath);
    }

    public IEnumerable<McpServerDescriptor> GetBuiltInServers()
    {
        if (_cachedServers != null) return _cachedServers;

        _cachedServers = new List<McpServerDescriptor>();

        if (!Directory.Exists(_toolKitsPath))
        {
            _logger.LogWarning("Local toolkits directory not found at {Path}", _toolKitsPath);
            // Fallback for development if needed, or just return empty
            return _cachedServers;
        }

        try
        {
            // Look for DLLs in subfolders
            var dllFiles = Directory.GetFiles(_toolKitsPath, "Mullai.MCP.*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                // Ensure it's not in a 'ref' or 'runtimes' folder
                if (dllFile.Contains(Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar) ||
                    dllFile.Contains(Path.DirectorySeparatorChar + "runtimes" + Path.DirectorySeparatorChar))
                    continue;

                var descriptor = CreateDescriptorFromDll(dllFile);
                if (descriptor != null)
                {
                    _cachedServers.Add(descriptor);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for built-in MCP servers");
        }

        return _cachedServers;
    }

    private McpServerDescriptor? CreateDescriptorFromDll(string dllPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(dllPath);
        var shortName = fileName.Replace("Mullai.MCP.", "");

        var descriptor = new McpServerDescriptor
        {
            Name = shortName,
            Type = "stdio",
            Command = "dotnet",
            Args = new[] { dllPath },
            Enabled = true,
            IsBuiltIn = true,
            Requirements = new List<McpConfigurationRequirement>()
        };

        try
        {
            // Use MetadataLoadContext to avoid locking the DLL or executing code during discovery
            // For simplicity in this implementation, we'll try to find the attributes by scanning the assembly
            var assembly = Assembly.LoadFrom(dllPath);
            foreach (var type in assembly.GetTypes())
            {
                var attrs = type.GetCustomAttributes(typeof(McpConfigurationRequirementAttribute), true);
                foreach (McpConfigurationRequirementAttribute attr in attrs)
                {
                    if (!descriptor.Requirements.Any(r => r.Key == attr.Key))
                    {
                        descriptor.Requirements.Add(new McpConfigurationRequirement
                        {
                            Key = attr.Key,
                            Description = attr.Description,
                            IsSecret = attr.IsSecret,
                            HelpUrl = attr.HelpUrl
                        });

                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not extract metadata from {DllPath}: {Message}", dllPath, ex.Message);
        }

        return descriptor;
    }
}

