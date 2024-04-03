using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InertiaCore.Utils
{
    public class DeferProp : LazyProp
    {
        public DeferProp(Func<object?> callback) : base(callback)
        {
        }
    }
}
