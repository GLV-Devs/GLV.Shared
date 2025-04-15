using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Storage.FTP;
public readonly record struct FTPProviderData(string Host, ushort Port, string? Username, string? Password, bool ValidateAnyCertificate = false);
