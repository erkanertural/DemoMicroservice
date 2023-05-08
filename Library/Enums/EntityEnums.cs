using System.ComponentModel;


namespace Library
{

    /// <summary>
    /// Kodsal tarafta varlığından haberdar olmak istediğim herşeyi FEATURE KOYUYORUZ , PARENT / PROP YAPISI İLK OLARAK FEATURE TABLOSUNA KOYUYORUZ.
    /// Feature tablosundaki Tablo adlarının Id karşılıkları
    /// </summary>

    public enum Features
    {

        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Banka bilgilerini {logo,mersisno} olarak tutabilen sınıftır.")]
        Bank = 4,
        [DescriptionML("Vergi dairesi bilgilerini {adres sınıfının ilçesine bağ kururak } olarak tutabilen sınıftır.")]
        TaxOffice = 7,
        [DescriptionML("Muhatap Tipi")]
        ContactType = 8,
        [DescriptionML("Adres")]
        Adres = 9,
        [DescriptionML("Günler")]
        Days = 10,
        [DescriptionML("Feature'ları validate etmek için kullanıyoruz")]
        ValidationRules = 11,
        [DescriptionML("Tablo")]
        Table = 21,
        [DescriptionML("Dil")]
        Language = 49,

        [DescriptionML("Hata Enumları")]
        ValidationErrorEnum = 25,
        [DescriptionML("-")]
        FormController = 43,
        [DescriptionML("Abone lisans bilgisi için lisansın hangi tipte olduğunu belirtmek için kullanılır.")]
        LicenseType = 58,
        [DescriptionML("Sektör")]
        Sector = 62,
        [DescriptionML("Alt Muhatap Bağlantı Türleri")]
        ContactRelation = 73,

        [DescriptionML("Ürün Tipleri")]
        ProductType = 83,

        [DescriptionML("Ürün Kategorileri")]
        ProductCategory = 84,
        [DescriptionML("Ürün Markaları")]
        ProductBrand = 85,
        [DescriptionML("Ürün Modelleri")]
        ProductModel = 86,
        [DescriptionML("Ürün Versiyonları")]
        ProductVersion = 140,
        [DescriptionML("Çalışan Sayısı")]
        ContactEmployeeCount = 87,
        [DescriptionML("Dil Desteği Anahtarları")]
        TranslateKey = 88,
        [DescriptionML("Dosya Türleri")]
        FileType = 89,
        [DescriptionML("Departman")]
        Department = 96,
        [DescriptionML("Para Birimleri")]
        CurrencyType = 101,
        [DescriptionML("Ölçü Birimleri")]
        MeasureOfUnit = 106,

        [DescriptionML("Benzer Ürünler")]
        ProductSimilar = 111,
        [DescriptionML("İlişkili Ürünler")]       // 1 ürün ile 2 nolu ProductSimilar
        ProductRelated = 112,

        [DescriptionML("Ürün Çoklu Kategori")]
        ProductMultiCategory = 113,

        [DescriptionML("Muhatap Bağlantı Verileri")]
        ContactRelationData = 2712,
        [DescriptionML("Ürün Reçete Verileri")]

        ProductPart = 114,
        [DescriptionML("Flex ")]
        FlexAction = 10001,

        [DescriptionML("Contact Tipi Tema", "")]
        ContactTypeSettings = 2713,

        [DescriptionML("Zimmmet Edilen")]
        ContactEmbezzled = 2718,
        [DescriptionML("Acil durum kişisi")]
        ContactEmergency = 2719,
        [DescriptionML("İstihdam durumu")]
        EmploymentEntry = 2720,
        [DescriptionML("İstihdam türü")]
        EmploymentType = 2721,
        [DescriptionML("İstihdam pozisyonu")]
        WorkPosition = 2722,
        [DescriptionML("Ayrılış türü")]
        ResignType = 2723,
        [DescriptionML("Kan grubu")]
        BloodGroup = 2724,
        [DescriptionML("Askerlik durumu")]
        MilitaryService = 2725,
        [DescriptionML("Evlilik durumu")]
        MaritalStatus = 2726,
        [DescriptionML("Engel durumu")]
        DisabilityStatus = 2727,
        [DescriptionML("Eş çalışma durumu")]
        SpouseJobStatus = 2728,
        [DescriptionML("Ürün Tedarikçi Verisi")]
        ProductSupplierData = 2755,
        [DescriptionML("Müşteri Ürün Takibi")]
        ContactProductTrack = 2756,
        [DescriptionML("Servis Olay durumları")]
        ServiceStatus = 2757,
        [DescriptionML("İş Emri Türü")]
        WorkOrderType = 2765,
        [DescriptionML("Arıza Nedeni")]
        FailureCause = 2766,
        [DescriptionML("Ürün Müşteri Takibi")]
        ProductContactTrack = 2767,
        [DescriptionML("Bağlı Ekipmanlar")]
        LinkedEquipment = 2768,


        [DescriptionML("Durum")]
        EventStatus = 2770,
        [DescriptionML("Durum Teması")]
        EventStatusSettings = 2772,
        [DescriptionML("Modüller")]
        Module = 2773,

    }


    public enum FeatureValidationErrorEnum
    {

   
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Request null olamaz.")]
        RequestCannotBeNull = 31,
        [DescriptionML("Object null olamaz.")]
        ObjectCannotBeNull = 82,
        [DescriptionML("Değer 0 olamaz.")]
        IdFieldCannotBeLtEqZero = 35,
        [DescriptionML("Alan boş bırakılamaz.")]
        FieldCannotBeEmptyOrNull = 36,

        [DescriptionML("Değer 0 dan büyük olmalıdır.")]
        PriceOrAmountValueMustGreaterThanZero = 37,

        [DescriptionML("Kullanıcı Adı veya Şifre Geçersiz")]
        UserNameOrPassswordInCorrect = 40,
        [DescriptionML("Kullanıcı Adı çok kısa")]
        UserNameTooShort = 42,
        [DescriptionML("Şifre çok kolay")]
        PasswordTooEasy = 44,

        [DescriptionML("Yetkiniz yok")]
        NotAuthorize = 130,
        [DescriptionML("Kullanıcı zaten kayıtlı")]
        UserAlreadyRegistered = 129,
        [DescriptionML("Zaten Silindi")]
        AlreadyDeleted = 131,

        [DescriptionML("Maksimum Değer Aşımı ")]
        MaximumValueError = 132,

        [DescriptionML("Minimum Değer Aşımı ")]
        MinimumValueError = 134,
        [DescriptionML("Minimum Uzunluk Aşımı ")]
        MinimumLengthError = 133,
        [DescriptionML("Maksimum Değer Aşımı ")]
        MaximumLengthError = 135,
        [DescriptionML("Geçersiz değer")]
        InvalidValueError = 136,
        [DescriptionML("Aralık Değerler Geçersiz")]
        BetweenValueError = 137,
        [DescriptionML("Kayıt Bulunamadı")]
        NotFoundRecord = 138,
        [DescriptionML("Kayıt Zaten Var")]
        AlreadExistRecord = 126,

        [DescriptionML("Güncelleme hatası")]
        UpdateError = 789654,
    }



 
   
    public enum FeatureCorporationEmployeeCount
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("1-5 Çalışan")]
        OneToFive = 90,
        [DescriptionML("5-10 Çalışan")]
        FiveToTen = 91,
        [DescriptionML("10-25 Çalışan")]
        TenToTwentyFive = 92,
        [DescriptionML("25-50 Çalışan")]
        TwentyFiveToFifty = 93,
        [DescriptionML("50-100 Çalışan")]
        FiftyToHundred = 94,
        [DescriptionML("100+ Çalışan")]
        HundredToMax = 95

    }
    public enum FeatureCurrencyType
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("TL")]
        TurkishLira = 102,
        [DescriptionML("€")]
        Euro = 103,
        [DescriptionML("$")]
        USDolar = 104,
        [DescriptionML("£")]
        Sterlin = 105,
    }
    public enum FeatureProductType
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,

        [DescriptionML("BulSearch")]
        Product = 10,
        [DescriptionML("Mail")]
        Accessories = 98,
        [DescriptionML("Drive")]
        Service = 99,

    }



    public enum FeatureAdres
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Ülke")]
        Country = 1,
        [DescriptionML("İl")]
        City = 2,
        [DescriptionML("İlçe")]
        District = 3,
    }

    public enum FeatureTranslateKey
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1
    
    }




    public enum FeatureDays
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Pazartesi")]
        Monday = 12,
    }
    public enum IsLogic
    {

        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Evet")]
        Yes = 1,
        [DescriptionML("Hayır")]
        No = 0,
    }
    public enum FeatureValidationRules
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Maksimum")]
        Maximum = 13,
        [DescriptionML("Minimum")]
        Minimum = 14,
        [DescriptionML("Arasında")]
        Between = 15,
        [DescriptionML("Karakter uzunluğu")]
        CharacterLength = 16,
        [DescriptionML("Gerekli mi?")]
        IsRequired = 17,
        [DescriptionML("Özel maske")]
        CustomInputMask = 18,
        [DescriptionML("Öntanımlı maske")]
        PredefinedInputMasks = 19,
        [DescriptionML("Sistemin sunduğu veya kullanıcının belirlediği \"çoktan seçmeli datalarda\" varsayılan hangisi seçili geleceğini tutacak olan özelliktir.")]
        DefaultSelection = 20,
    }


   
 

    public enum FeatureTable
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Contact")]
        Contact = 22,
        [DescriptionML("Subscriber")]
        Subscriber = 23,
        [DescriptionML("Product")]
        Product = 613516,
        [DescriptionML("Component")]
        Component = 116551,
        [DescriptionML("Feature")]
        Feature = 11655452,
        [DescriptionML("FeatureValue")]
        FeatureValue = 116253,
        [DescriptionML("ContactPerson")]
        ContactPerson = 2769
    }

    public enum FeatureLanguage
    {
        [DescriptionML("Belirsiz")]
        Uncertain = -1,
        [DescriptionML("Türkçe")]
        Tr = 50,
        [DescriptionML("En-Us")]
        EnUs = 51,
        [DescriptionML("En-UK")]
        EnUK = 52,
        [DescriptionML("Es")]
        Es = 53,
        [DescriptionML("It")]
        It = 54,
        [DescriptionML("Fr")]
        Fr = 55,
        [DescriptionML("De")]
        De = 56,
        [DescriptionML("Bulgarca")]
        Bg = 57,

    }

    public enum OperationType
    {
        [DescriptionML("Create")]
        C,
        [DescriptionML("Update")]
        U,
        [DescriptionML("Delete")]
        D,
        [DescriptionML("SoftDelete")]
        S

    }
}
