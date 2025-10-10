using System;using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IToolDiscoveryService
{
    IEnumerable<ToolInfo> GetTools();
}
