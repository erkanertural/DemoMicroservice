namespace Library
{
    public class ExtendWithAttribute : Attribute
    {
        public Type Type{ get; set; }
        public ExtendWithAttribute(Type type)
        {
            Type = type;
        }
    }
}
