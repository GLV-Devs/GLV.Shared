using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.CosmosDB.Options;

public sealed class CosmosDBOptions
{
    public string? Endpoint { get; set; }
    public string? AuthKeyOrResourceToken { get; set; }
    public string? DatabaseName { get; set; }
}
