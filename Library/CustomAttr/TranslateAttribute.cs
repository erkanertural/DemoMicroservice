using Library;

namespace Library
{
    public class TranslateAttribute : Attribute
    {
        public FeatureTranslateKey TranslateKey { get; set; }
        public TranslateAttribute(FeatureTranslateKey translateKey)
        {
            TranslateKey = translateKey;
            
        }
    }
}
