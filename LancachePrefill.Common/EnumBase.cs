namespace LancachePrefill.Common
{
    /// <summary>
    /// This class is intended to be used to create "strongly typed enums", as an alternative to regular "int" enums in C#.
    /// The main goal is to avoid "stringly" typed functions that take in a large number of string parameters.  Frequently, these parameters
    /// are usually constrained to only a few values, making them ideal for enums.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static members are required for class' implementation")]
    public abstract class EnumBase<T> where T : EnumBase<T>
    {
        private static readonly List<T> _allEnumValues = new List<T>();

        // Despite analysis tool warnings, we want this static bool to be on this generic type (so that each T has its own bool).
        private static bool _invoked; //NOSONAR - See above message

        private static object _lockObject = new object();

        
        public static List<T> AllEnumValues
        {
            get
            {
                lock (_lockObject)
                {
                    if (!_invoked)
                    {
                        _invoked = true;
                        // Force initialization by calling one of the derived fields/properties.  Failure to do this will result in this list being empty.
                        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(p => p.PropertyType == typeof(T))?.GetValue(null, null);
                        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(p => p.FieldType == typeof(T))?.GetValue(null);
                    }
                    return _allEnumValues;
                }
            }
        }

        public string Name { get; }

        protected EnumBase(string name)
        {
            Name = name;
            AllEnumValues.Add(this as T);
        }

        /// <summary>
        /// Used to parse a value type, into the strongly typed "enum" equivalent.
        ///
        /// Throws an exception if an invalid value is passed in.
        /// </summary>
        /// <param name="toParse"></param>
        /// <returns>A strongly typed "enum" equivalent.</returns>
        public static T Parse(string toParse)
        {
            for (var index = 0; index < AllEnumValues.Count; index++)
            {
                var type = AllEnumValues[index];
                if (toParse == type.Name)
                {
                    return type;
                }
            }
            throw new FormatException($"{toParse} is not a valid enum value for {typeof(T).Name}!");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}