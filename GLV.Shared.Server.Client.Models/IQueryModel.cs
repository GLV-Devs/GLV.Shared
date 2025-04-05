using System.Collections;
using System.Text;
using System.Xml.Linq;

namespace GLV.Shared.Server.Client.Models;

public interface IQueryModel
{
    public string ToQueryString(StringBuilder? sb);
}
