using System;

namespace UnityAutopilot.Tools
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ToolParamAttribute : Attribute
    {
        public string Description { get; }
        public bool IsRequired { get; } = true;

        public ToolParamAttribute(string description, bool isRequired = false)
        {
            Description = description;
            IsRequired = isRequired;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ToolParamEnumAttribute : Attribute
    {
        public string[] EnumValues { get; } = Array.Empty<string>();

        public ToolParamEnumAttribute(params string[] enumValue)
        {
            EnumValues = enumValue;
        }

        public ToolParamEnumAttribute(Type enumType)
        {
            if (enumType.IsEnum)
            {
                EnumValues = Enum.GetNames(enumType);
            }
            else
            {
                throw new ArgumentException("Type must be an enum");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ToolParamMultiTypeAttribute : Attribute
    {
        public Type[] Types { get; }
        public ToolParamMultiTypeAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}