using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLL.Contracts
{
    internal interface IEntityAdapter<T>
    {
        T Adapt(object[] values);
    }
}
