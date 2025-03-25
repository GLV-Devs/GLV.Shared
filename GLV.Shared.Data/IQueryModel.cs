using System.Collections;
using System.Text;
using System.Xml.Linq;

namespace GLV.Shared.Data;

public interface IQueryModel
{
    public string ToQueryString();
}
