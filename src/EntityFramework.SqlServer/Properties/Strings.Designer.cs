// <auto-generated />
namespace Microsoft.Data.Entity.SqlServer
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Resources;
	using JetBrains.Annotations;

    public static class Strings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("EntityFramework.SqlServer.Strings", typeof(Strings).GetTypeInfo().Assembly);

        /// <summary>
        /// The value provided for argument '{argumentName}' must be a valid value of enum type '{enumType}'.
        /// </summary>
        public static string InvalidEnumValue([CanBeNull] object argumentName, [CanBeNull] object enumType)
        {
            return string.Format(CultureInfo.CurrentCulture, GetString("InvalidEnumValue", "argumentName", "enumType"), argumentName, enumType);
        }

        /// <summary>
        /// The increment value of '{increment}' for sequence '{sequenceName}' cannot be used for value generation. Sequences used for value generation must have positive increments.
        /// </summary>
        public static string SequenceBadBlockSize([CanBeNull] object increment, [CanBeNull] object sequenceName)
        {
            return string.Format(CultureInfo.CurrentCulture, GetString("SequenceBadBlockSize", "increment", "sequenceName"), increment, sequenceName);
        }

        /// <summary>
        /// Identity value generation cannot be used for the property '{property}' on entity type '{entityType}' because the property type is '{propertyType}'. Identity value generation can only be used with signed integer properties.
        /// </summary>
        public static string IdentityBadType([CanBeNull] object property, [CanBeNull] object entityType, [CanBeNull] object propertyType)
        {
            return string.Format(CultureInfo.CurrentCulture, GetString("IdentityBadType", "property", "entityType", "propertyType"), property, entityType, propertyType);
        }

        /// <summary>
        /// SQL Server sequences cannot be used to generate values for the property '{property}' on entity type '{entityType}' because the property type is '{propertyType}'. Sequences can only be used with integer properties.
        /// </summary>
        public static string SequenceBadType([CanBeNull] object property, [CanBeNull] object entityType, [CanBeNull] object propertyType)
        {
            return string.Format(CultureInfo.CurrentCulture, GetString("SequenceBadType", "property", "entityType", "propertyType"), property, entityType, propertyType);
        }

        /// <summary>
        /// SQL Server-specific methods can only be used when the context is using a SQL Server data store.
        /// </summary>
        public static string SqlServerNotInUse
        {
            get { return GetString("SqlServerNotInUse"); }
        }

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);

            Debug.Assert(value != null);

            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }

            return value;
        }
    }
}
