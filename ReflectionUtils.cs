using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Healthbars
{
    static class ReflectionUtils
    {
        public static T GetPrivateField<T>(this object obj, string field)
        {
            Type t = obj.GetType();
            if (t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentException($"The field {field} does not exist in target class {t.Name}!");
            }
            return (T)t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        }
    }
}
