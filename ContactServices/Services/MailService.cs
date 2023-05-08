using Bulmail.Core;
using Bulmail.Core.Helpers;
using Bulmail.Core.Services;
using Bulmail.Core.ViewModels;
using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Dynamic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Upsilon.Services.Services
{
    public class MailService : Bulmail.Core.Services.IMailService
    {
        private IUnitOfWork _unitOfWork;
        private IUserService _userService;
        protected readonly IConfiguration Configuration;
        private IHttpContextAccessor _httpContextAccessor;
        HtmlFormatter htmlFormatter = new HtmlFormatter();
        Base64Formatter Base64Formatter = new Base64Formatter();
        CultureInfo culture = new CultureInfo("tr-TR");
        private string Ip = "";
        string myEnv = "";
        public MailService(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork, IUserService userService)
        {
            myEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{myEnv}.json", false)
                .Build();
            _unitOfWork = unitOfWork;

            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> SendMail(SendMailViewModel vm)
        {
            var result = new Result();
            try
            {

                if (vm.Subject == null && vm.Body == null && vm.Receivers == null)
                {
                    result.Success = false;
                    result.Message = "One or more fields are required!";
                    return result;
                }

                //Debugger.Break();
                vm.Password = GetPassword(vm.Sender.Replace("armaara.com", "bul.com.tr"));
                // create email message
                var mail = new MimeMessage();

                string from = vm.Sender;
                if (myEnv == "dev")
                {
                    from = from.Replace("bul.com.tr", Configuration.GetSection("domain").Value);
                }
                var userResult = await _userService.Get(vm.Sender);

                mail.From.Add(MailboxAddress.Parse(userResult.Success ? '"' + userResult.Data.FirstName + " " + userResult.Data.LastName + '"' + "<" + from + ">" : from));

                // add receivers
                foreach (string receiver in vm.Receivers ?? Enumerable.Empty<string>())
                {
                    mail.To.Add(MailboxAddress.Parse(receiver));
                }
                // add CCs
                foreach (string? CC in vm.CC ?? Enumerable.Empty<string>())
                {
                    mail.Cc.Add(MailboxAddress.Parse(CC));
                }
                // add BCCs
                foreach (string? BCC in vm.BCC ?? Enumerable.Empty<string>())
                {
                    mail.Bcc.Add(MailboxAddress.Parse(BCC));
                }
                var doc = new HtmlDocument();
                doc.LoadHtml(vm.Body);  // Load your html text here
                mail.Importance = (MessageImportance)(vm.MessageImportance != null ? vm.MessageImportance : MessageImportance.Normal);
                mail.Subject = vm.Subject ?? "";
                var builder = new BodyBuilder();
                try
                {
                    foreach (var node in doc.DocumentNode.SelectNodes("//img"))
                    {
                        // File path to the image. We get the src attribute off the current node for the file name.

                        var base64 = node.GetAttributeValue("src", "");
                        // base64 string get contentType

                        var byteArr = Convert.FromBase64String(base64.Split(',').Last());

                        // Set content type to the current image's extension, such as "png" or "jpg"
                        var contentType = new ContentType("image", ".png");
                        var contentId = MimeKit.Utils.MimeUtils.GenerateMessageId();
                        var image = (MimePart)builder.LinkedResources.Add(Guid.NewGuid().ToString(), byteArr);
                        image.ContentTransferEncoding = ContentEncoding.Base64;
                        image.ContentId = contentId;
                        image.ContentDisposition = new ContentDisposition() { Disposition = ContentDisposition.Inline };
                        // Set the current image's src attriubte to "cid:<content-id>"
                        node.SetAttributeValue("src", $"cid:" + contentId);
                    }
                }
                catch (Exception)
                {
                }




                // add Files
                var validPathList = new string[] { ".ade", ".adp", ".apk", ".appx", ".appxbundle", ".bat",
                    ".cab", ".chm", ".cmd", ".com", ".cpl", ".dll", ".dmg", ".ex", ".ex_", ".exe", ".hta", ".ins", ".isp",
                    ".iso", ".jar", ".js", ".jse", ".lib", ".lnk", ".mde", ".msc", ".msi", ".msix", ".msixbundle", ".msp",
                    ".mst", ".nsh", ".pif", ".ps1", ".scr", ".sct", ".shb", ".sys", ".vb", ".vbe", ".vbs", ".vxd", ".wsc", ".wsf", ".wsh"
                };

                if (vm.Files != null)
                {
                    foreach (var file in vm.Files)
                    {
                        if (file.Length > 0)
                        {
                            if (file.Length < 25 * 1024 * 1024)
                            {
                                if (!validPathList.Contains(Path.GetExtension(file.FileName).ToLower()))
                                {
                                    builder.Attachments.Add(file.FileName, file.OpenReadStream());
                                }
                                else
                                {
                                    result.Success = false;
                                    result.Message = "File path not supported ! " + file.FileName;
                                    return result;
                                }
                            }
                            else
                            {
                                result.Success = false;
                                result.Message = "File size cannot be larger than 25 MB ! " + file.FileName;
                                return result;
                            }
                        }
                    }
                }
                foreach (MimeEntity attachment in builder.Attachments ?? Enumerable.Empty<MimeEntity>())
                {
                    var contentId = MimeKit.Utils.MimeUtils.GenerateMessageId();
                    attachment.ContentId = contentId;
                }
                builder.HtmlBody = doc.DocumentNode.OuterHtml;
                mail.Body = builder.ToMessageBody();

                // send email
                if (!vm.Draft)
                {
                    using var smtp = new SmtpClient();

                    string smtpServer = Configuration.GetSection("MailSettings:smtpServer").Value;
                    int smtpPort = Convert.ToInt32(Configuration.GetSection("MailSettings:smtpPort").Value);

                    await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.None);
                    if (myEnv == "dev")
                    {
                        await smtp.AuthenticateAsync(vm.Sender.Replace("bul.com.tr", "armaara.com"), vm.Password);
                    }
                    else
                    {
                        await smtp.AuthenticateAsync(vm.Sender, vm.Password);
                    }
                    long mailSize = 0;
                    using (var stream = new MimeKit.IO.MeasuringStream())
                    {
                        await mail.WriteToAsync(stream);
                        mailSize = stream.Length;
                    }
                    if (smtp.MaxSize < mailSize)
                    {
                        result.Success = false;
                        result.Message = "Mail size greater than max limit!";
                        return result;
                    }

                    await smtp.SendAsync(mail);
                    await smtp.DisconnectAsync(true);
                    //await SendSaveDb(vm);

                    // delete draft 
                    if (vm.DraftMailId != null)
                    {
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            Result folders = null;
                            if (myEnv == "dev")
                            {
                                folders = await GetMailFolders(vm.Sender.Replace("bul.com.tr", Configuration.GetSection("domain").Value));
                            }
                            else
                            {
                                folders = await GetMailFolders(vm.Sender);
                            }

                            //json to obj


                            var deserializedFolders = JObject.Parse(folders.Message);
                            var folderList = deserializedFolders["Folders"].Select(x => new { id = x["id"], name = x["name"] }).ToArray();
                            var draftFolder = folderList.FirstOrDefault(x => x.name.ToString() == "Drafts");


                            var res = await client.DeleteAsync("/users/" + userResult.Data.WildDuckId + "/mailboxes/" + draftFolder.id + "/messages/" + vm.DraftMailId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);

                        }


                        //using (var client = new ImapClient())
                        //{
                        //    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                        //    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort").Value);

                        //    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.StartTls);
                        //    // var oauth2 = new SaslMechanismOAuth2(vm.Sender, "token");
                        //    //client.Authenticate(oauth2);
                        //    await client.AuthenticateAsync(vm.Sender, vm.Password);

                        //    var sentFolder = client.GetFolder(SpecialFolder.Drafts);

                        //    // complately delete mail from Draft folder by id
                        //    var draftFolder = client.GetFolder(SpecialFolder.Drafts);
                        //    await draftFolder.OpenAsync(FolderAccess.ReadWrite);
                        //    var draftMailIdList = new List<UniqueId>();
                        //    var uid = UniqueId.Parse(vm.DraftMailId);
                        //    draftMailIdList.Add(uid);
                        //    await draftFolder.AddFlagsAsync(uid, MessageFlags.Deleted, true);
                        //    await draftFolder.ExpungeAsync(draftMailIdList);

                        //}
                    }
                }
                else if (vm.IsDraftUpdate)
                {
                    //@todo update draft mail


                    //object msg = new
                    //{
                    //    unseen = false,
                    //    draft = true,
                    //    flagged = false,
                    //    from = new
                    //    {
                    //        name = userResult != null ? userResult.Data.Name + " " + userResult.Data.Surname : "",
                    //        address = vm.Sender
                    //    },
                    //    to = vm.Receivers?.Select(x => new { name = "", address = x }).ToArray(),
                    //    cc = vm.CC?.Select(x => new { name = "", address = x }).ToArray(),
                    //    bcc = vm.BCC?.Select(x => new { name = "", address = x }).ToArray(),
                    //    subject = vm.Subject,
                    //    raw = mail.Body,
                    //    attachments = mail.Attachments?.Select(x => new { filename = Base64Formatter.Format((MimePart)x)["FileName"], content = Base64Formatter.Format((MimePart)x)["Content"], contentType = Base64Formatter.Format((MimePart)x)["ContentType"], cid = MimeKit.Utils.MimeUtils.GenerateMessageId() }).ToArray()

                    //};

                    //using (var client = new HttpClient())
                    //{
                    //    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    //    client.DefaultRequestHeaders.Accept.Clear();
                    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //    var json = JsonConvert.SerializeObject(msg);
                    //    var data = new StringContent(json, Encoding.UTF8, "application/json");

                    //    await client.PutAsync("/users/" + userResult.Data.DuckId + "/mailboxes/Drafts/" + vm.DraftMailId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, data);

                    //}




                    //using (var client = new ImapClient())
                    //{
                    //    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    //    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort").Value);

                    //    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.StartTls);
                    //    // var oauth2 = new SaslMechanismOAuth2(vm.Sender, "token");
                    //    //client.Authenticate(oauth2);
                    //    await client.AuthenticateAsync(vm.Sender, vm.Password);

                    //    var draftFolder = client.GetFolder(SpecialFolder.Drafts);

                    //    await draftFolder.OpenAsync(FolderAccess.ReadWrite
                    //         );
                    //    var draftMsg = await draftFolder.GetMessageAsync(UniqueId.Parse(vm.DraftMailId.ToString()));

                    //    draftMsg.To.Clear();
                    //    //try
                    //    //{
                    //    //    mail.To.OfType<MailboxAddress>().Single().Address = "Taslak";
                    //    //}
                    //    //catch (Exception)
                    //    //{
                    //    //    mail.To.Add(MailboxAddress.Parse("Taslak"));
                    //    //}
                    //    foreach (string? receiver in vm.Receivers ?? Enumerable.Empty<string>())
                    //    {
                    //        if (receiver != "Taslak") { draftMsg.To.Add(MailboxAddress.Parse(receiver)); };
                    //    }

                    //    draftMsg.Cc.Clear();
                    //    draftMsg.Bcc.Clear();

                    //    // add CCs
                    //    foreach (string? CC in vm.CC ?? Enumerable.Empty<string>())
                    //    {
                    //        draftMsg.Cc.Add(MailboxAddress.Parse(CC));
                    //    }
                    //    // add BCCs
                    //    foreach (string? BCC in vm.BCC ?? Enumerable.Empty<string>())
                    //    {
                    //        draftMsg.Bcc.Add(MailboxAddress.Parse(BCC));
                    //    }

                    //    draftMsg.Subject = vm.Subject;
                    //    draftMsg.Date = DateTime.Now;
                    //    builder = new BodyBuilder();
                    //    builder.HtmlBody = vm.Body;

                    //    draftMsg.Body = builder.ToMessageBody();
                    //    ReplaceRequest r = new ReplaceRequest(draftMsg, MessageFlags.Draft | MessageFlags.Seen);

                    //    draftFolder.Replace(UniqueId.Parse(vm.DraftMailId.ToString()), r);
                    //    await draftFolder.AddFlagsAsync(UniqueId.Parse(vm.DraftMailId.ToString()), MessageFlags.Seen, true);
                    //    await client.DisconnectAsync(true);

                    //}


                }
                else  // Save Draft
                {
                    //object msg = new
                    //{
                    //    user = userResult.Data.DuckId,
                    //    unseen = false,
                    //    draft = true,
                    //    flagged = false,
                    //    from = new
                    //    {
                    //        name = userResult != null ? userResult.Data.Name + " " + userResult.Data.Surname : "",
                    //        address = vm.Sender
                    //    },
                    //    to = vm.Receivers?.Select(x => new { name = "", address = x }).ToArray(),
                    //    cc = vm.CC?.Select(x => new { name = "", address = x }).ToArray(),
                    //    bcc = vm.BCC?.Select(x => new { name = "", address = x }).ToArray(),
                    //    subject = vm.Subject,
                    //    html = vm.Body,

                    //    //set obj


                    //    //attachments = mail.Attachments?.Select(x => new { filename = Base64Formatter.Format((MimePart)x)["FileName"], content = Base64Formatter.Format((MimePart)x)["Content"], contentType = Base64Formatter.Format((MimePart)x)["ContentType"], cid = MimeKit.Utils.MimeUtils.GenerateMessageId() }).ToArray()

                    //};

                    dynamic msg = new ExpandoObject();
                    msg.user = userResult.Data.WildDuckId;
                    msg.unseen = false;
                    msg.draft = true;
                    msg.flagged = false;
                    msg.from = new
                    {
                        name = userResult != null ? userResult.Data.FirstName + " " + userResult.Data.LastName : "",
                        address = vm.Sender
                    };

                    msg.to = vm.Receivers?.Select(x => new { name = "", address = x }).ToArray();

                    msg.subject = vm.Subject;
                    msg.html = vm.Body;


                    if (vm.CC != null)
                    {
                        msg.cc = vm.CC?.Select(x => new { name = "", address = x }).ToArray();
                    }
                    if (vm.BCC != null)
                    {
                        msg.bcc = vm.BCC?.Select(x => new { name = "", address = x }).ToArray();
                    }
                    if (mail.Attachments.Count() > 0)
                    {
                        msg.attachments = mail.Attachments?.Select(x => new { filename = Base64Formatter.Format((MimePart)x)["FileName"], content = Base64Formatter.Format((MimePart)x)["Content"], contentType = Base64Formatter.Format((MimePart)x)["ContentType"], cid = MimeKit.Utils.MimeUtils.GenerateMessageId() }).ToArray();
                    }
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var json = JsonConvert.SerializeObject(msg);
                        var data = new StringContent(json, Encoding.UTF8, "application/json");

                        Result folders = null;
                        if (myEnv == "dev")
                        {
                            folders = await GetMailFolders(vm.Sender.Replace("bul.com.tr", Configuration.GetSection("domain").Value));
                        }
                        else
                        {
                            folders = await GetMailFolders(vm.Sender);
                        }

                        //json to obj


                        var deserializedFolders = JObject.Parse(folders.Message);
                        var folderList = deserializedFolders["Folders"].Select(x => new { id = x["id"], name = x["name"] }).ToArray();
                        var draftFolder = folderList.FirstOrDefault(x => x.name.ToString() == "Drafts");


                        //TODO
                        // get users drafts folders id
                        var res = await client.PostAsync("/users/" + userResult.Data.WildDuckId + "/mailboxes/" + draftFolder.id + "/messages?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, data);

                    }
                    // append to sent mail folder
                    //using (var client = new ImapClient())
                    //{
                    //    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    //    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort").Value);

                    //    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.StartTls);
                    //    // var oauth2 = new SaslMechanismOAuth2(vm.Sender, "token");
                    //    //client.Authenticate(oauth2);
                    //    await client.AuthenticateAsync(vm.Sender, vm.Password);

                    //    var sentFolder = client.GetFolder(SpecialFolder.Drafts);

                    //    if (sentFolder != null)
                    //    {
                    //        mail.To.Clear();
                    //        //try
                    //        //{
                    //        //    mail.To.OfType<MailboxAddress>().Single().Address = "Taslak";
                    //        //}
                    //        //catch (Exception)
                    //        //{                          
                    //        //    mail.To.Add(MailboxAddress.Parse("Taslak"));
                    //        //   // mail.To.OfType<MailboxAddress>().Single().Address = "Taslak";
                    //        //}
                    //        foreach (string? receiver in vm.Receivers ?? Enumerable.Empty<string>())
                    //        {
                    //            if (receiver != "Taslak") { mail.To.Add(MailboxAddress.Parse(receiver)); };
                    //        }

                    //        // add CCs
                    //        mail.Cc.Clear();
                    //        foreach (string? CC in vm.CC ?? Enumerable.Empty<string>())
                    //        {
                    //            mail.Cc.Add(MailboxAddress.Parse(CC));
                    //        }
                    //        // add BCCs
                    //        mail.Bcc.Clear();
                    //        foreach (string? BCC in vm.BCC ?? Enumerable.Empty<string>())
                    //        {
                    //            mail.Bcc.Add(MailboxAddress.Parse(BCC));
                    //        }

                    //        await sentFolder.OpenAsync(FolderAccess.ReadWrite);
                    //        await sentFolder.AppendAsync(mail);
                    //        var uids = sentFolder.Search(SearchQuery.NotSeen);
                    //        foreach (var uid in uids)
                    //        {
                    //            await sentFolder.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    //            await sentFolder.AddFlagsAsync(uid, MessageFlags.Draft, true);
                    //        }
                    //    }

                    //    await client.DisconnectAsync(true);

                    //}

                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Sender + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + result.Message + " | " + " Ip= " + Ip);
                return result;
            }


        }




        // add flag status
        public async Task<Result> AddFlagStatus(MailFlagStatus vm)
        {
            var result = new Result();
            try
            {
                using (var client = new ImapClient())
                {
                    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                    vm.Password = GetPassword(vm.Mail);

                    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync(vm.Mail, vm.Password);

                    var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);
                    IMailFolder folder;
                    try
                    {
                        folder = await topLevel.GetSubfolderAsync(vm.FolderName);
                    }
                    catch (Exception)
                    {
                        var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
                        await tempFolder.OpenAsync(FolderAccess.ReadOnly);
                        folder = await tempFolder.GetSubfolderAsync(vm.FolderName);
                    }
                    try
                    {
                        await folder.OpenAsync(FolderAccess.ReadWrite);
                    }
                    catch (Exception)
                    {
                    }



                    IList<UniqueId> uniqueIds = new List<UniqueId>();

                    foreach (var mailId in vm.MailIds)
                    {
                        uniqueIds.Add(UniqueId.Parse(mailId.ToString()));
                    }
                    var mails = await folder.FetchAsync(uniqueIds, MessageSummaryItems.All);

                    foreach (var mail in mails)
                    {
                        await folder.AddFlagsAsync(mail.UniqueId, vm.Flag, true);
                        //var originalMail = await folder.GetMessageAsync(mail.UniqueId);
                        if (mail.Flags.Value.HasFlag(MessageFlags.Draft))
                        {
                            //var m = await folder.GetMessageAsync(mail.UniqueId);
                            try
                            {
                                mail.Envelope.To.OfType<MailboxAddress>().Single().Address = "Taslak";
                            }
                            catch (Exception)
                            {
                            }

                            await folder.AddFlagsAsync(mail.UniqueId, MessageFlags.Seen, true);
                        }

                        if (vm.Flag == MessageFlags.Flagged)
                        {

                            bool oldIsRead = mail.Flags.Value.HasFlag(MessageFlags.Seen) || false;

                            var flaggedFolder = await client.GetFolderAsync("Flagged");
                            try
                            {
                                await flaggedFolder.OpenAsync(FolderAccess.ReadWrite);
                            }
                            catch (Exception)
                            {
                            }

                            var flaggedFoldersMail = await flaggedFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", mail.Envelope.MessageId));

                            if (flaggedFoldersMail.Count == 0)
                            { // clone to flaggedFolder
                                try
                                {
                                    await folder.OpenAsync(FolderAccess.ReadWrite);
                                }
                                catch (Exception)
                                {
                                }

                                await folder.CopyToAsync(mail.UniqueId, flaggedFolder);
                                try
                                {
                                    await flaggedFolder.OpenAsync(FolderAccess.ReadWrite);
                                }
                                catch (Exception)
                                {
                                }

                                var copied = flaggedFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", mail.Envelope.MessageId)).Result.FirstOrDefault();

                                if (oldIsRead)
                                {

                                    await flaggedFolder.AddFlagsAsync(copied, MessageFlags.Seen, true);
                                }

                                //await flaggedFolder.AppendAsync(originalMail, oldIsRead ? MessageFlags.Seen : MessageFlags.None);

                                var notFlaggeds = await flaggedFolder.SearchAsync(SearchQuery.NotFlagged);
                                foreach (var notFlagged in notFlaggeds)
                                {
                                    await flaggedFolder.AddFlagsAsync(UniqueId.Parse(notFlagged.Id.ToString()), MessageFlags.Flagged, true);
                                }

                            }
                        }

                    }
                    await client.DisconnectAsync(true);
                }
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // remove flag status
        public async Task<Result> RemoveFlagStatus(MailFlagStatus vm)
        {
            var result = new Result();
            try
            {
                using (var client = new ImapClient())
                {
                    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                    vm.Password = GetPassword(vm.Mail);

                    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync(vm.Mail, vm.Password);

                    var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);
                    IMailFolder folder;
                    try
                    {
                        folder = await topLevel.GetSubfolderAsync(vm.FolderName);
                    }
                    catch (Exception)
                    {
                        var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
                        try
                        {
                            await tempFolder.OpenAsync(FolderAccess.ReadOnly);
                        }
                        catch (Exception)
                        {
                        }

                        folder = await tempFolder.GetSubfolderAsync(vm.FolderName);
                    }
                    try
                    {
                        await folder.OpenAsync(FolderAccess.ReadWrite);
                    }
                    catch (Exception)
                    {
                    }



                    IList<UniqueId> uniqueIds = new List<UniqueId>();

                    foreach (var mailId in vm.MailIds)
                    {
                        uniqueIds.Add(UniqueId.Parse(mailId.ToString()));
                    }
                    var mails = await folder.FetchAsync(uniqueIds, MessageSummaryItems.All);

                    foreach (var mail in mails)
                    {
                        await folder.RemoveFlagsAsync(mail.UniqueId, vm.Flag, true);

                        // var mainMail = await folder.GetMessageAsync(mail.UniqueId);
                        if (vm.Flag == MessageFlags.Flagged && vm.FolderName == "Flagged")
                        {
                            await folder.AddFlagsAsync(mail.UniqueId, MessageFlags.Deleted, true);
                            await folder.ExpungeAsync();

                            //await topLevel.CreateAsync("tempFlag", false);
                            //var tempFolder = topLevel.GetSubfolder("tempFlag");

                            //await folder.MoveToAsync(mail.UniqueId, tempFolder);
                            //await tempFolder.OpenAsync(FolderAccess.ReadWrite);

                            //await tempFolder.ExpungeAsync();
                            //await tempFolder.CloseAsync();
                            //await tempFolder.DeleteAsync();

                            /* remove original mails flag */

                            var allFolders = client.GetFoldersAsync(client.PersonalNamespaces[0]).Result.Where(x => x.Name != "Flagged");

                            foreach (var f in allFolders)
                            {
                                try
                                {
                                    try
                                    {
                                        await f.OpenAsync(FolderAccess.ReadWrite);
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    var flaggedFoldersMail = f.Search(SearchQuery.HeaderContains("Message-Id", mail.Envelope.MessageId));
                                    if (flaggedFoldersMail.Count > 0)
                                    {
                                        await f.RemoveFlagsAsync(UniqueId.Parse(flaggedFoldersMail[0].Id.ToString()), MessageFlags.Flagged, true);
                                    }
                                    await f.CloseAsync();
                                }
                                catch (Exception)
                                {

                                }

                            }

                        }
                        if (vm.Flag == MessageFlags.Flagged && vm.FolderName != "Flagged")
                        {

                            string mainMailId = mail.Envelope.MessageId;
                            var flaggedFolder = await client.GetFolderAsync("Flagged");
                            try
                            {
                                await flaggedFolder.OpenAsync(FolderAccess.ReadWrite);
                            }
                            catch (Exception)
                            {
                            }

                            var flaggedFoldersMail = flaggedFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", mainMailId)).Result;

                            if (flaggedFoldersMail.Count > 0)
                            {
                                await flaggedFolder.AddFlagsAsync(UniqueId.Parse(flaggedFoldersMail.FirstOrDefault().Id.ToString()), MessageFlags.Deleted, true);
                                await flaggedFolder.ExpungeAsync();
                            }

                            //await topLevel.CreateAsync("tempFlag", false);
                            //var tempFolder = await topLevel.GetSubfolderAsync("tempFlag");

                            //await flaggedFolder.MoveToAsync(UniqueId.Parse(flaggedFoldersMail.Id.ToString()), tempFolder);
                            //await tempFolder.OpenAsync(FolderAccess.ReadWrite);

                            //await tempFolder.ExpungeAsync();
                            //await tempFolder.CloseAsync();
                            //await tempFolder.DeleteAsync();
                        }

                    }
                    await client.DisconnectAsync(true);
                }
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }
        public async Task<Result> SetSeenOrUnseen(SeenViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    dynamic data = new ExpandoObject();
                    data.seen = vm.IsSeen;

                    var json = JsonConvert.SerializeObject(data);
                    var dataContent = new StringContent(json, Encoding.UTF8, "application/json");

                    res = client.PutAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages/" + vm.MailId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent).Result;

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = JsonConvert.SerializeObject(JToken.Parse(res.Content.ReadAsStringAsync().Result));
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }

        }

        public async Task<Result> SetFlaggedOrUnflagged(FlaggedViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    dynamic data = new ExpandoObject();
                    data.flagged = vm.IsFlagged;

                    var json = JsonConvert.SerializeObject(data);
                    var dataContent = new StringContent(json, Encoding.UTF8, "application/json");

                    res = client.PutAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages/" + vm.MailId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent).Result;

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = JsonConvert.SerializeObject(JToken.Parse(res.Content.ReadAsStringAsync().Result));
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }

        }


        //Get email detail
        public async Task<Result> EmailDetail(GetMailViewModel vm)
        {
            var result = new Result();
            JObject mail;
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));
                //Result folders = null;
                //if (myEnv == "dev")
                //{
                //    folders = await GetMailFolders(vm.Mail.Replace("bul.com.tr", Configuration.GetSection("domain").Value));
                //}
                //else
                //{
                //    folders = await GetMailFolders(vm.Mail);
                //}
                //var deserializedFolders = JObject.Parse(folders.Message);
                //var folderList = deserializedFolders["Folders"].Select(x => new { id = x["id"], name = x["name"] }).ToArray();
                //var selectedFolder = folderList.FirstOrDefault(x => x.name.ToString().ToLower() == vm.FolderName.ToLower());


                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    res = client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages/" + vm.MailId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value).Result;


                    if (res.IsSuccessStatusCode)
                    {
                        var jsonContent = JToken.Parse(res.Content.ReadAsStringAsync().Result);
                        //get object data
                        var attachments = jsonContent["attachments"];

                        if (attachments.HasValues)
                        {
                            foreach (var attachment in attachments)
                            {
                                var base64Data = await DownloadAttachmentWithId(new DownloadFileViewModel { Mail = vm.Mail, MailId = vm.MailId, FolderId = vm.FolderId, AttachmentId = attachment["id"].ToString() });
                                var html = jsonContent["html"][0];
                                jsonContent["html"][0] = html.ToString().Replace("attachment:" + attachment["id"].ToString(), "data:" + "image/jpeg" + ";base64," + base64Data.Data.ToString());
                            }
                        }
                        jsonContent["date"] = DateTime.Parse(jsonContent["date"].ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture);

                        result.Message = JsonConvert.SerializeObject(jsonContent);
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;

                //using (var client = new ImapClient())
                //{
                //    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                //    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                //    vm.Password = GetPassword(vm.Mail);

                //    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.SslOnConnect);
                //    await client.AuthenticateAsync(vm.Mail, vm.Password);

                //    var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);
                //    IMailFolder folder;
                //    try
                //    {
                //        folder = await topLevel.GetSubfolderAsync(vm.FolderName);
                //    }
                //    catch (Exception)
                //    {
                //        var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
                //        await tempFolder.OpenAsync(FolderAccess.ReadOnly);
                //        folder = tempFolder.GetSubfolder(vm.FolderName);
                //    }
                //    await folder.OpenAsync(FolderAccess.ReadOnly);

                //    //var message = folder.GetMessage(UniqueId.Parse(vm.MailId.ToString()));

                //    IList<UniqueId> uniqueIds = new List<UniqueId>();
                //    uniqueIds.Add(UniqueId.Parse(vm.MailId.ToString()));
                //    var fetchedMail = folder.FetchAsync(uniqueIds, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).Result.FirstOrDefault();

                //    List<Object> attachmentList = new List<Object>();

                //    TextPart html = new TextPart();
                //    try
                //    {
                //        html = (TextPart)folder.GetBodyPartAsync(fetchedMail.UniqueId, fetchedMail.HtmlBody).Result;
                //    }
                //    catch (Exception)
                //    {
                //        html = (TextPart)folder.GetBodyPartAsync(fetchedMail.UniqueId, fetchedMail.TextBody).Result;
                //    }
                //    //1276
                //    var bodyParts = fetchedMail?.BodyParts.ToList() ?? new List<BodyPartBasic>();

                //    //get all cid values in string
                //    var cidList = new List<string>();
                //    foreach (var item in bodyParts)
                //    {
                //        if (item.ContentId != null)
                //        {
                //            if (html.Text.Contains(item.ContentId.Replace("<", "").Replace(">", "")))
                //            {
                //                try
                //                {
                //                    var img = await folder.GetBodyPartAsync(fetchedMail.UniqueId, item);
                //                    var mimeFile = (MimePart)img;
                //                    var file = Base64Formatter.Format(mimeFile);

                //                    html.Text = html.Text.Replace("cid:" + img.ContentId, "data:" + "image/jpeg" + ";base64," + file.Value<string>("Content"));

                //                }
                //                catch (Exception)
                //                {
                //                }

                //            }

                //        }

                //        if (item.ContentTransferEncoding == "base64" && item.FileName != null)
                //        {

                //            if (item.FileName.Contains("."))
                //            {
                //                attachmentList.Add(item);
                //            }
                //        }

                //    }

                //for (int i = 0; i < bodyParts.Count(); i++)
                //{
                //    if (bodyParts[i].ContentTransferEncoding == "base64" && bodyParts[i].FileName != null)
                //    {
                //        var img =await folder.GetBodyPartAsync(fetchedMail.UniqueId, bodyParts[i]);
                //        var mimeFile = (MimePart)img;
                //        var file = Base64Formatter.Format(mimeFile);


                //        if (img.ContentId != null)
                //        {
                //            html.Text = html.Text.Replace("cid:" + img.ContentId, "data:" + "image/jpeg" + ";base64," + file.Value<string>("Content"));
                //        }
                //        if (file.Value<string>("FileName") != null && mimeFile.FileName.Contains("."))
                //        {
                //            attachmentList.Add(file);
                //        }



                //        //if (vm.FolderName == "Drafts")
                //        //{
                //        //    var img = folder.GetBodyPart(fetchedMail.UniqueId, bodyParts[i]);
                //        //    attachmentList.Add(Base64Formatter.Format((MimePart)img));
                //        //}
                //        //else
                //        //{
                //        //    attachmentList.Add(bodyParts[i]);
                //        //}

                //    }
                //}


                //    mail = new JObject
                //    {
                //        ["Id"] = Convert.ToInt32(vm.MailId.ToString()),
                //        ["From"] = fetchedMail.Envelope.From.ToString(),
                //        ["To"] = fetchedMail.Envelope.To.ToString(),
                //        ["Subject"] = fetchedMail.Envelope.Subject,
                //        ["Body"] = html.Text,
                //        ["Date"] = Convert.ToDateTime(fetchedMail.Envelope.Date.ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture),
                //        ["BCC"] = fetchedMail.Envelope.Bcc.ToString(),
                //        ["CC"] = fetchedMail.Envelope.Cc.ToString(),
                //        ["Attachments"] = JToken.Parse(JsonConvert.SerializeObject(attachmentList)),
                //        ["IsRead"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Seen),
                //        ["IsFlagged"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged),
                //        ["IsAnswered"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Answered),
                //        ["IsDraft"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Draft),
                //        ["IsDeleted"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Deleted),
                //        ["IsRecent"] = fetchedMail.Flags.Value.HasFlag(MessageFlags.Recent),
                //        ["References"] = JToken.Parse(JsonConvert.SerializeObject(fetchedMail.References)),
                //        ["Folder"] = vm.FolderName
                //    };

                //}
                //result.Success = true;
                //result.Message = JsonConvert.SerializeObject(new JObject { ["Mail"] = JToken.Parse(JsonConvert.SerializeObject(mail)) });
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // get mail folders

        public async Task<Result> GetMailFolders(string email)
        {
            var result = new Result();
            var user = await _userService.Get(email.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var res = client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value).Result;

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = JsonConvert.SerializeObject(new JObject { ["Folders"] = JToken.Parse(res.Content.ReadAsStringAsync().Result)["results"] });
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + user.Data.Email + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }


        // create mail folder
        public async Task<Result> CreateMailFolder(CreateFolderViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    dynamic data = new ExpandoObject();
                    data.path = vm.FolderName;

                    var json = JsonConvert.SerializeObject(data);
                    var dataContent = new StringContent(json, Encoding.UTF8, "application/json");

                    var res = await client.PostAsync("/users/" + user.Data.WildDuckId + "/mailboxes?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent);

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = $"{vm.FolderName} created.";
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Folder not created!";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // remove mail folder
        public async Task<Result> RemoveMailFolder(RemoveFolderViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var res = await client.DeleteAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = "Folder deleted.";
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Folder not deleted!";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }
        // Download attachment
        public async Task<Result<string>> DownloadAttachmentWithId(DownloadFileViewModel vm)
        {
            var result = new Result<string>();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var res = await client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages/" + vm.MailId + "/attachments/" + vm.AttachmentId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);

                    if (res.IsSuccessStatusCode)
                    {
                        var image = await res.Content.ReadAsByteArrayAsync();
                        string base64 = Convert.ToBase64String(image);
                        result.Data = base64;
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Folder not deleted!";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }
        // Folder rename
        public async Task<Result> RenameMailFolder(RenameFolderViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    dynamic data = new ExpandoObject();
                    data.path = vm.NewFolderName;

                    var json = JsonConvert.SerializeObject(data);
                    var dataContent = new StringContent(json, Encoding.UTF8, "application/json");

                    var res = await client.PutAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent);

                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = "Folder name updated.";
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Folder name not changed!";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }


        // get mails with folder name
        public async Task<Result> GetMailsWithFolderId(MailFolderViewModel vm)
        {
            var result = new Result();
            var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));
            //Result folders = null;
            //if (myEnv == "dev")
            //{
            //    folders = await GetMailFolders(vm.Mail.Replace("bul.com.tr", Configuration.GetSection("domain").Value));
            //}
            //else
            //{
            //    folders = await GetMailFolders(vm.Mail);
            //}
            //var deserializedFolders = JObject.Parse(folders.Message);
            //var folderList = deserializedFolders["Folders"].Select(x => new { id = x["id"], name = x["name"] }).ToArray();
            //var selectedFolder = folderList.FirstOrDefault(x => x.name.ToString().ToLower() == vm.FolderName.ToLower());

            //if (selectedFolder == null)
            //{
            //    result.Success = false;
            //    result.Message = "Folder not found";
            //    return result;
            //}
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;
                    if (vm.Next != null && vm.Next.Trim() != "")
                    {
                        res = client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value
                        + "&limit=50&" + "order=desc&next=" + vm.Next).Result;
                    }
                    else if (vm.Previous != null && vm.Previous.Trim() != "")
                    {
                        res = client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value
                        + "&limit=50&" + "order=desc&previous=" + vm.Previous).Result;
                    }
                    else
                    {
                        res = client.GetAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value
                         + "&limit=50&" + "order=desc").Result;
                    }


                    if (res.IsSuccessStatusCode)
                    {
                        var jsonContent = JToken.Parse(res.Content.ReadAsStringAsync().Result);
                        foreach (var mail in jsonContent["results"])
                        {
                            mail["date"] = DateTime.Parse(mail["date"].ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture);
                        }
                        result.Message = JsonConvert.SerializeObject(jsonContent);
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // mail move
        public async Task<Result> Move(MoveViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));


                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    dynamic data = new ExpandoObject();
                    data.moveTo = vm.NewFolderId;

                    for (int i = 0; i < vm.MailIds.Count; i++)
                    {
                        data.message = vm.MessageIds[i];
                        var json = JsonConvert.SerializeObject(data);
                        var dataContent = new StringContent(json, Encoding.UTF8, "application/json");
                        res = client.PutAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.OldFolderId + "/messages/" + vm.MailIds[i] + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent).Result;
                    }

                    result.Message = "Mails moved successfully";
                    result.Success = true;
                    return result;

                }
                result.Success = false;
                result.Message = "Error";
                return result;

                //using (var client = new ImapClient())
                //{
                //    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                //    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                //    vm.Password = GetPassword(vm.Mail);

                //    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.SslOnConnect);

                //    await client.AuthenticateAsync(vm.Mail, vm.Password);
                //    await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
                //    var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);

                //    IMailFolder oldFolder;
                //    IMailFolder newFolder;
                //    try
                //    {
                //        oldFolder = await topLevel.GetSubfolderAsync(vm.OldFolder);
                //    }
                //    catch (Exception)
                //    {
                //        var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
                //        await tempFolder.OpenAsync(FolderAccess.ReadOnly);
                //        oldFolder = await tempFolder.GetSubfolderAsync(vm.OldFolder);
                //    }


                //    try
                //    {
                //        newFolder = await topLevel.GetSubfolderAsync(vm.NewFolder);
                //    }
                //    catch (Exception)
                //    {
                //        var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
                //        await tempFolder.OpenAsync(FolderAccess.ReadOnly);
                //        newFolder = await tempFolder.GetSubfolderAsync(vm.NewFolder);
                //    }

                //    foreach (var mailId in vm.MailIds)
                //    {
                //        await oldFolder.OpenAsync(FolderAccess.ReadWrite);
                //        //var mail = await oldFolder.GetMessageAsync(UniqueId.Parse(mailId.ToString()));
                //        var fetchedMail = oldFolder.FetchAsync(new[] { UniqueId.Parse(mailId.ToString()) }, MessageSummaryItems.All).Result.FirstOrDefault();
                //        bool oldIsRead = fetchedMail.Flags.Value.HasFlag(MessageFlags.Seen) || false;
                //        bool oldIsFlagged = fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged) || false;


                //        await oldFolder.MoveToAsync(UniqueId.Parse(mailId.ToString()), newFolder);
                //        await oldFolder.AddFlagsAsync(UniqueId.Parse(mailId.ToString()), MessageFlags.Seen, true);
                //        await oldFolder.AddFlagsAsync(UniqueId.Parse(mailId.ToString()), MessageFlags.Deleted, true);
                //        await oldFolder.ExpungeAsync();
                //        await oldFolder.CloseAsync();

                //        await newFolder.OpenAsync(FolderAccess.ReadWrite);
                //        var fetchedNewMailId = newFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", fetchedMail.Envelope.MessageId)).Result.FirstOrDefault();
                //        var fetchedNewMail = newFolder.FetchAsync(new[] { fetchedNewMailId }, MessageSummaryItems.All).Result.FirstOrDefault();



                //        if (oldFolder.Name == "Drafts" && newFolder.Name == "Trash")
                //        {
                //            try
                //            {
                //                fetchedNewMail.Envelope.To.OfType<MailboxAddress>().Single().Address = "Taslak";
                //            }
                //            catch (Exception)
                //            {
                //            }
                //        }
                //        var flaggedFoldersMail = newFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", fetchedMail.Envelope.MessageId)).Result.FirstOrDefault();

                //        var list = new List<UniqueId>();
                //        list.Add(UniqueId.Parse(flaggedFoldersMail.Id.ToString()));
                //        if (oldFolder.Name == "Flagged" && fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged) && newFolder.Name == "Trash")
                //        {
                //            var allFolders = client.GetFoldersAsync(client.PersonalNamespaces[0]).Result.Where(x => x.Name != "Flagged" && x.Name != newFolder.Name);

                //            foreach (var f in allFolders)
                //            {
                //                try
                //                {
                //                    await f.OpenAsync(FolderAccess.ReadWrite);
                //                    var searchMail = f.SearchAsync(SearchQuery.HeaderContains("Message-Id", fetchedNewMail.Envelope.MessageId)).Result.FirstOrDefault();
                //                    if (searchMail != null)
                //                    {
                //                        await f.RemoveFlagsAsync(UniqueId.Parse(searchMail.Id.ToString()), MessageFlags.Flagged, true);
                //                        await f.AddFlagsAsync(UniqueId.Parse(searchMail.Id.ToString()), MessageFlags.Deleted, true);
                //                        var flaggedDelUids = new List<UniqueId>();
                //                        flaggedDelUids.Add(UniqueId.Parse(searchMail.Id.ToString()));
                //                        await f.ExpungeAsync(flaggedDelUids);
                //                    }
                //                    await f.CloseAsync();
                //                }
                //                catch (Exception)
                //                {

                //                }
                //            }
                //            await newFolder.OpenAsync(FolderAccess.ReadWrite);
                //        }

                //        else if (newFolder.Name != "Flagged" && newFolder.Name != "Trash" && fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged))
                //        {
                //            await newFolder.AddFlagsAsync(list, MessageFlags.Flagged, true);
                //        }
                //        else if (newFolder.Name != "Trash" && fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged))
                //        {
                //            await newFolder.RemoveFlagsAsync(list, MessageFlags.Flagged, true);
                //        }
                //        else if (oldFolder.Name != "Flagged" && fetchedMail.Flags.Value.HasFlag(MessageFlags.Flagged) && newFolder.Name == "Trash")
                //        {
                //            await newFolder.RemoveFlagsAsync(list, MessageFlags.Flagged, true);
                //            var flaggedFolder = client.GetFolder("Flagged");
                //            await flaggedFolder.OpenAsync(FolderAccess.ReadWrite);

                //            var searchMail = flaggedFolder.SearchAsync(SearchQuery.HeaderContains("Message-Id", fetchedNewMail.Envelope.MessageId)).Result.FirstOrDefault();

                //            var oldUids = new List<UniqueId>();
                //            oldUids.Add(UniqueId.Parse(searchMail.Id.ToString()));
                //            await flaggedFolder.AddFlagsAsync(oldUids, MessageFlags.Deleted, true);

                //            await flaggedFolder.ExpungeAsync(oldUids);
                //        }

                //    }

                //    await client.DisconnectAsync(true);

                //    result.Message = "Mails moved successfully";
                //    result.Success = true;
                //    return result;
                //}
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }



        //Mail forward
        public async Task<Result> Forward(ForwardMailViewModel vm)
        {
            var result = new Result();
            try
            {
                using (var client = new ImapClient())
                {
                    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                    vm.Password = GetPassword(vm.Mail);

                    await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.SslOnConnect);

                    await client.AuthenticateAsync(vm.Mail, vm.Password);
                    await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
                    var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);

                    IMailFolder folder;
                    try
                    {
                        folder = topLevel.GetSubfolder(vm.FolderName);
                    }
                    catch (Exception)
                    {
                        var tempFolder = topLevel.GetSubfolder("UserCreatedFolders");
                        tempFolder.Open(FolderAccess.ReadOnly);
                        folder = tempFolder.GetSubfolder(vm.FolderName);
                    }

                    await folder.OpenAsync(FolderAccess.ReadWrite);

                    var mainMail = await folder.GetMessageAsync(UniqueId.Parse(vm.MailId.ToString()));

                    // create new email message
                    var mail = new MimeMessage();

                    if (!mainMail.Subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase))
                        mail.Subject = "Fwd: " + mainMail.Subject;
                    else
                        mail.Subject = mainMail.Subject;


                    mail.From.Add(MailboxAddress.Parse(vm.Mail));

                    // add receivers
                    foreach (string receiver in vm.Receivers)
                    {
                        mail.To.Add(MailboxAddress.Parse(receiver));
                    }
                    // add CCs
                    foreach (string? CC in vm.CC ?? Enumerable.Empty<string>())
                    {
                        mail.Cc.Add(MailboxAddress.Parse(CC));
                    }
                    // add BCCs
                    foreach (string? BCC in vm.BCC ?? Enumerable.Empty<string>())
                    {
                        mail.Bcc.Add(MailboxAddress.Parse(BCC));
                    }
                    var builder = new BodyBuilder();

                    builder.HtmlBody = vm.Body;

                    if (builder.HtmlBody == null)
                        builder.TextBody = vm.Body;


                    // add Files
                    if (mainMail.Attachments != Enumerable.Empty<string>())
                    {
                        foreach (var attachment in mainMail.Attachments)
                        {
                            builder.Attachments.Add(attachment);
                        }
                    }
                    mail.Body = builder.ToMessageBody();

                    // send email
                    using var smtp = new SmtpClient();

                    string smtpServer = Configuration.GetSection("MailSettings:smtpServer").Value;
                    int smtpPort = Convert.ToInt32(Configuration.GetSection("MailSettings:smtpPort").Value);

                    await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(vm.Mail, vm.Password);
                    await smtp.SendAsync(mail);
                    await smtp.DisconnectAsync(true);
                    await client.DisconnectAsync(true);

                }
                result.Success = true;
                result.Message = "Mail forwarded successfully";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        public Result MailSuggest(AuthenticationViewModel vm)
        {
            var result = new Result();
            var fromMailList = new List<string>();
            try
            {
                using (var client = new ImapClient())
                {
                    string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
                    int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

                    vm.Password = GetPassword(vm.Mail);

                    client.Connect(imapServer, imapPort, SecureSocketOptions.SslOnConnect);
                    client.Authenticate(vm.Mail, vm.Password);
                    var folder = client.GetFolder(SpecialFolder.Sent);
                    folder.Open(FolderAccess.ReadWrite);

                    fromMailList = folder.Where(x => x.From.Contains(MailboxAddress.Parse(vm.Mail))).OrderByDescending(x => x.Date).Select(x => x.To.First().ToString()).Distinct().ToList();
                    fromMailList.Remove("\"'Mail Delivery System'\" <MAILER-DAEMON@bul.com.tr>");

                    folder.Close();
                    client.Disconnect(true);
                }
                result.Message = JsonConvert.SerializeObject(new JObject { ["MailList"] = JToken.Parse(JsonConvert.SerializeObject(fromMailList)) });
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }

        }

        //idle 
        public void Idle(AuthenticationViewModel vm)
        {
            string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
            int imapPort = 143; //Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort993").Value);

            vm.Password = GetPassword(vm.Mail);

            using (var client = new IdleClient(imapServer, imapPort, SecureSocketOptions.StartTlsWhenAvailable, vm.Mail, vm.Password))
            {
                var idleTask = client.RunAsync();
                Task.Run(() =>
                {
                    Console.ReadKey(true);
                });
                idleTask.GetAwaiter().GetResult();

            }
        }

        public async Task<Result> SearchEmails(SearchViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    string startDate = "";

                    if (vm.DayAgo != null && vm.DayAgo != 0)
                    {
                        startDate = DateTime.Today.AddDays((double)-vm.DayAgo).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'Z'");
                    }

                    var query = new Dictionary<string, string>()
                    {
                        ["accessToken"] = Configuration.GetSection("WildDuckAccessToken").Value,
                        ["mailbox"] = vm.FolderId ?? "",
                        ["query"] = vm.Query ?? "",
                        ["from"] = vm.From ?? "",
                        ["to"] = vm.To ?? "",
                        ["or.to"] = vm.Cc ?? "",
                        ["subject"] = vm.Subject ?? "",
                        ["flagged"] = vm.Flagged?.ToString() ?? "",
                        ["unseen"] = vm.Unseen?.ToString() ?? "",
                        ["datestart"] = startDate,
                        ["limit"] = vm.PageSize.ToString(),
                        ["next"] = vm.Next ?? "",
                        ["previous"] = vm.Previous ?? ""
                    };

                    var removedNulls = (from kv in query
                                        where kv.Value != null && kv.Value != ""
                                        select kv).ToDictionary(kv => kv.Key, kv => kv.Value);

                    var url = QueryHelpers.AddQueryString("/users/" + user.Data.WildDuckId + "/search", removedNulls);
                    res = client.GetAsync(url).Result;


                    if (res.IsSuccessStatusCode)
                    {
                        var jsonContent = JToken.Parse(res.Content.ReadAsStringAsync().Result);
                        foreach (var mail in jsonContent["results"])
                        {
                            mail["date"] = DateTime.Parse(mail["date"].ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture);
                        }
                        result.Message = JsonConvert.SerializeObject(jsonContent);
                        result.Success = true;
                        return result;
                    }
                }

                result.Success = false;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        //public async Task<Result> GetSearchEmails(SearchViewModel vm)
        //{
        //    var result = new Result();
        //    try
        //    {
        //        List<JObject> mailList = new List<JObject>();
        //        List<JObject> folders = new List<JObject>();
        //        var paginateData = new JObject();
        //        int unreadCount = 0;

        //        using (var client = new ImapClient())
        //        {
        //            string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
        //            int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort").Value);

        //            vm.Password = GetPassword(vm.Mail);

        //            await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.StartTls);
        //            await client.AuthenticateAsync(vm.Mail, vm.Password);

        //            await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
        //            var topLevel = await client.GetFolderAsync(client.PersonalNamespaces[0].Path);

        //            IMailFolder folder;
        //            try
        //            {
        //                folder = await topLevel.GetSubfolderAsync(vm.FolderName);
        //            }
        //            catch (Exception)
        //            {
        //                var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
        //                await tempFolder.OpenAsync(FolderAccess.ReadOnly);
        //                folder = await tempFolder.GetSubfolderAsync(vm.FolderName);
        //            }

        //            await folder.OpenAsync(FolderAccess.ReadOnly);
        //            var orderBy = new[] { OrderBy.ReverseArrival };
        //            SearchQuery query = new SearchQuery();

        //            if (!string.IsNullOrEmpty(vm.SearchBody))
        //            {
        //                query = query.And(SearchQuery.BodyContains(vm.SearchBody));
        //            }

        //            if (!string.IsNullOrEmpty(vm.Subject))
        //            {
        //                query = query.And(SearchQuery.SubjectContains(vm.Subject));
        //            }
        //            if (!string.IsNullOrEmpty(vm.To))
        //            {
        //                query = query.And(SearchQuery.ToContains(vm.To));
        //            }
        //            if (!string.IsNullOrEmpty(vm.From))
        //            {
        //                query = query.And(SearchQuery.FromContains(vm.From));
        //            }
        //            DateTime time = DateTime.Now - TimeSpan.FromDays(1);
        //            //Value of selected time in days
        //            if (vm.DayOfRange > 0)
        //            {
        //                time = DateTime.Now - TimeSpan.FromDays(vm.DayOfRange);
        //            }

        //            var uids = await folder.SearchAsync(query);
        //            //foreach (var item in uids)
        //            //{
        //            //    var message = folder.GetMessageAsync(item).Result.Body;

        //            //}
        //            var mails = folder.FetchAsync(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).Result;

        //            var dateFilterMails = mails.Where(x => x.Date >= time);
        //            // order desc uids
        //            var orderedMails = dateFilterMails.OrderByDescending(x => x.UniqueId);
        //            unreadCount = orderedMails.Where(x => x.Flags.Value.HasFlag(MessageFlags.Seen) == false).Count();
        //            // mail pagination
        //            int pageSize = 50;
        //            int pageNumber = vm.Page;
        //            int totalPages = (int)Math.Ceiling(mails.Count() / (double)pageSize);

        //            var paginatedMails = orderedMails.Skip((pageNumber - 1) * pageSize).Take(pageSize);


        //            paginateData["PageNumber"] = pageNumber;
        //            paginateData["TotalPages"] = totalPages;
        //            paginateData["PageSize"] = pageSize;
        //            paginateData["TotalCount"] = orderedMails.Count();

        //            foreach (var m in paginatedMails)
        //            {
        //                //var message = folder.GetMessage(m.UniqueId);

        //                List<Object> attachmentList = new List<Object>();
        //                var bodyParts = m.BodyParts.ToList();
        //                for (int i = 0; i < bodyParts.Count(); i++)
        //                {
        //                    if (bodyParts[i].ContentTransferEncoding == "base64" || bodyParts[i].FileName != null)
        //                    {
        //                        attachmentList.Add(bodyParts[i]);
        //                    }
        //                }


        //                TextPart html = new TextPart();
        //                try
        //                {
        //                    html = (TextPart)folder.GetBodyPartAsync(m.UniqueId, m.HtmlBody).Result;
        //                }
        //                catch (Exception)
        //                {

        //                }
        //                mailList.Add(new JObject
        //                {
        //                    ["Id"] = Convert.ToInt32(m.UniqueId.ToString()),
        //                    ["From"] = m.Envelope.From.ToString(),
        //                    ["To"] = m.Envelope.To.ToString(),
        //                    ["Subject"] = m.Envelope.Subject,
        //                    ["Body"] = m.TextBody != null ? Regex.Replace(((TextPart)folder.GetBodyPartAsync(m.UniqueId, m?.TextBody).Result).Text?.Replace(Environment.NewLine, String.Empty).Replace("*", " "), "<.*?>", String.Empty) : Regex.Replace(html.Text.Replace(Environment.NewLine, String.Empty), "<.*?>", String.Empty).Replace("&gt;", String.Empty).Replace("&lt;", String.Empty),
        //                    ["Date"] = Convert.ToDateTime(m.Envelope.Date.ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture),
        //                    ["BCC"] = m.Envelope.Bcc.ToString(),
        //                    ["CC"] = m.Envelope.Cc.ToString(),
        //                    ["Attachments"] = JToken.Parse(JsonConvert.SerializeObject(attachmentList)),
        //                    ["IsRead"] = m.Flags.Value.HasFlag(MessageFlags.Seen),
        //                    ["IsFlagged"] = m.Flags.Value.HasFlag(MessageFlags.Flagged),
        //                    ["IsAnswered"] = m.Flags.Value.HasFlag(MessageFlags.Answered),
        //                    ["IsDraft"] = m.Flags.Value.HasFlag(MessageFlags.Draft),
        //                    ["IsDeleted"] = m.Flags.Value.HasFlag(MessageFlags.Deleted),
        //                    ["IsRecent"] = m.Flags.Value.HasFlag(MessageFlags.Recent),
        //                    ["InReplyTo"] = m.Envelope.InReplyTo,
        //                    ["Folder"] = folder.Name,
        //                });
        //            }

        //            await client.DisconnectAsync(true);
        //        }

        //        result.Message = JsonConvert.SerializeObject(new JObject { ["PaginateData"] = paginateData, ["UnreadCount"] = unreadCount, ["Mails"] = JToken.Parse(JsonConvert.SerializeObject(mailList)) });
        //        result.Success = true;

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result.Success = false;
        //        result.Message = ex.Message;
        //        Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        //        if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        //            Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
        //        Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
        //        return result;
        //    }
        //}

        //public async Task<Result> GetSearchEmailsAllFolder(SearchViewModel vm)
        //{
        //    var result = new Result();
        //    try
        //    {
        //        List<JObject> mailList = new List<JObject>();
        //        List<JObject> folders = new List<JObject>();
        //        var folderListAll = new List<string>() { "Inbox", "Sent" };
        //        var paginateData = new JObject();
        //        int unreadCount = 0;

        //        using (var client = new ImapClient())
        //        {
        //            string imapServer = Configuration.GetSection("MailSettings:imapServer").Value;
        //            int imapPort = Convert.ToInt32(Configuration.GetSection("MailSettings:imapPort").Value);

        //            vm.Password = GetPassword(vm.Mail);

        //            await client.ConnectAsync(imapServer, imapPort, SecureSocketOptions.StartTls);
        //            await client.AuthenticateAsync(vm.Mail, vm.Password);

        //            await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
        //            var topLevel = client.GetFolder(client.PersonalNamespaces[0]);


        //            foreach (var folderItem in folderListAll)
        //            {
        //                IMailFolder folder;
        //                try
        //                {
        //                    folder = await topLevel.GetSubfolderAsync(folderItem);
        //                }
        //                catch (Exception)
        //                {
        //                    var tempFolder = await topLevel.GetSubfolderAsync("UserCreatedFolders");
        //                    await tempFolder.OpenAsync(FolderAccess.ReadOnly);
        //                    folder = await tempFolder.GetSubfolderAsync(folderItem);
        //                }

        //                await folder.OpenAsync(FolderAccess.ReadOnly);
        //                var orderBy = new[] { OrderBy.ReverseArrival };

        //                SearchQuery query = SearchQuery.BodyContains(vm.SearchBody);

        //                if (!string.IsNullOrEmpty(vm.Subject))
        //                {
        //                    query = query.And(SearchQuery.SubjectContains(vm.Subject));
        //                }
        //                if (!string.IsNullOrEmpty(vm.To))
        //                {
        //                    query = query.And(SearchQuery.ToContains(vm.To));
        //                }
        //                if (!string.IsNullOrEmpty(vm.From))
        //                {
        //                    query = query.And(SearchQuery.FromContains(vm.From));
        //                }
        //                DateTime time = DateTime.Now - TimeSpan.FromDays(1);
        //                //Value of selected time in days
        //                if (vm.DayOfRange > 0)
        //                {
        //                    time = DateTime.Now - TimeSpan.FromDays(vm.DayOfRange);
        //                }

        //                var uids = await folder.SearchAsync(query);
        //                //foreach (var item in uids)
        //                //{
        //                //    var message = folder.GetMessageAsync(item).Result.Body;

        //                //}
        //                var mails = await folder.FetchAsync(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);

        //                var dateFilterMails = mails.Where(x => x.Date >= time);
        //                // order desc uids
        //                var orderedMails = dateFilterMails.OrderByDescending(x => x.UniqueId);
        //                unreadCount = orderedMails.Where(x => x.Flags.Value.HasFlag(MessageFlags.Seen) == false).Count();
        //                // mail pagination
        //                int pageSize = 50;
        //                int pageNumber = vm.Page;
        //                int totalPages = (int)Math.Ceiling(mails.Count() / (double)pageSize);

        //                var paginatedMails = orderedMails.Skip((pageNumber - 1) * pageSize).Take(pageSize);


        //                paginateData["PageNumber"] = pageNumber;
        //                paginateData["TotalPages"] = totalPages;
        //                paginateData["PageSize"] = pageSize;

        //                foreach (var uid in paginatedMails)
        //                {
        //                    var message = await folder.GetMessageAsync(uid.UniqueId);

        //                    List<JObject> attachmentList = new List<JObject>();

        //                    foreach (var attachment in message.Attachments)
        //                    {
        //                        // covert to base64
        //                        attachmentList.Add(Base64Formatter.Format((MimePart)attachment));
        //                    }
        //                    mailList.Add(new JObject
        //                    {
        //                        ["Id"] = Convert.ToInt32(uid.UniqueId.ToString()),
        //                        ["From"] = message.From.ToString(),
        //                        ["To"] = message.To.ToString(),
        //                        ["Subject"] = message.Subject,
        //                        ["Body"] = message.TextBody != null ? Regex.Replace(message?.TextBody?.Replace(Environment.NewLine, string.Empty).Replace("*", " "), "<.*?>", String.Empty) : Regex.Replace(message.HtmlBody?.Replace(Environment.NewLine, String.Empty), "<.*?>", String.Empty),
        //                        ["Date"] = Convert.ToDateTime(message.Date.ToString()).ToString("dd MMMM yyyy, HH:mm, dddd, dd.MM.yyyy", culture),
        //                        ["BCC"] = message.Bcc.ToString(),
        //                        ["CC"] = message.Cc.ToString(),
        //                        ["Attachments"] = JToken.Parse(JsonConvert.SerializeObject(attachmentList)),
        //                        ["IsRead"] = uid.Flags.Value.HasFlag(MessageFlags.Seen),
        //                        ["IsFlagged"] = uid.Flags.Value.HasFlag(MessageFlags.Flagged),
        //                        ["IsAnswered"] = uid.Flags.Value.HasFlag(MessageFlags.Answered),
        //                        ["IsDraft"] = uid.Flags.Value.HasFlag(MessageFlags.Draft),
        //                        ["IsDeleted"] = uid.Flags.Value.HasFlag(MessageFlags.Deleted),
        //                        ["IsRecent"] = uid.Flags.Value.HasFlag(MessageFlags.Recent),
        //                        ["Importance"] = message.Importance.ToString(),
        //                        ["References"] = JToken.Parse(JsonConvert.SerializeObject(message.References)),
        //                        ["InReplyTo"] = message.InReplyTo,
        //                        ["Folder"] = "Inbox",
        //                    });
        //                }

        //                if (folderItem.Equals(folderListAll[^1]))
        //                {
        //                    //folders = GetMailFolders(client);
        //                    await client.DisconnectAsync(true);
        //                }

        //            }

        //            result.Message = JsonConvert.SerializeObject(new JObject { ["PaginateData"] = paginateData, ["UnreadCount"] = unreadCount, ["Folders"] = JToken.Parse(JsonConvert.SerializeObject(folders)), ["Mails"] = JToken.Parse(JsonConvert.SerializeObject(mailList)) });
        //            result.Success = true;

        //            return result;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.Success = false;
        //        result.Message = ex.Message;
        //        Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        //        if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        //            Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
        //        Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
        //        return result;
        //    }
        //}

        public string GetPassword(string email)
        {
            return AesOperation.EncryptString(Configuration.GetSection("AesCryptKey").Value, email);
        }

        public async Task<Result> RemoveMailFromDeletedFolder(DeleteMailViewModel vm)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(vm.Mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    for (int i = 0; i < vm.MailIds.Count; i++)
                    {
                        await client.DeleteAsync("/users/" + user.Data.WildDuckId + "/mailboxes/" + vm.FolderId + "/messages/" + vm.MailIds[i] + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);
                    }

                    result.Success = true;
                    result.Message = $"{vm.MailIds.Count} message deleted successfully.";
                    return result;

                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + vm.Mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // get user quota
        public async Task<Result> UserQuota(string mail)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var res = await client.GetAsync("/users/" + user.Data.WildDuckId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);

                    if (res.IsSuccessStatusCode)
                    {
                        var objectData = JObject.Parse(await res.Content.ReadAsStringAsync());
                        var quota = objectData["limits"]["quota"];

                        result.Message = quota.ToString();
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Quota information not found!";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        // Get disable information
        public async Task<Result> GetMailSyncSettings(string mail)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    res = client.GetAsync("/users/" + user.Data.WildDuckId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value).Result;


                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = JsonConvert.SerializeObject(new JObject
                        {
                            ["DisabledScopes"] = JToken.Parse(res.Content.ReadAsStringAsync().Result)["disabledScopes"],
                            ["ImapSettings"] = new JObject { ["ImapServer"] = Configuration.GetSection("MailSettings:imapServer").Value, ["ImapPort"] = Configuration.GetSection("MailSettings:imapPort").Value },
                            ["PopSettings"] = new JObject { ["PopServer"] = Configuration.GetSection("MailSettings:popServer").Value, ["PopPort"] = Configuration.GetSection("MailSettings:popPort").Value },
                            ["SmtpSettings"] = new JObject { ["SmtpServer"] = Configuration.GetSection("MailSettings:smtpServer").Value, ["SmtpPort"] = Configuration.GetSection("MailSettings:smtpPort").Value }
                        });


                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        public async Task<Result> DisableScopes(string mail, string[]? scopes)
        {
            var result = new Result();
            try
            {
                var user = await _userService.Get(mail.Replace(Configuration.GetSection("domain").Value, "bul.com.tr"));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = null;

                    dynamic data = new ExpandoObject();
                    data.disabledScopes = scopes?.Select(x => x).ToArray();

                    var json = JsonConvert.SerializeObject(data);
                    var dataContent = new StringContent(json, Encoding.UTF8, "application/json");

                    res = client.PutAsync("/users/" + user.Data.WildDuckId + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, dataContent).Result;


                    if (res.IsSuccessStatusCode)
                    {
                        result.Message = "Success";
                        result.Success = true;
                        return result;
                    }
                }
                result.Success = false;
                result.Message = "Error";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + mail + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }
    }
}
