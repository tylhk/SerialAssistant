using System;

namespace WPFSerialAssistant
{
    [Serializable] // 关键序列化标记
    public class CommandItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}