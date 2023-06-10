using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;

namespace DGJv3
{
    class VersionChecker
    {
        #region -- 常量 --

        /// <summary>
        /// 默认服务器地址
        /// </summary>
        public const string DEFAULT_BASE_URL = "https://www.danmuji.org";
        public const string GITHUB_owner = "luiguangguan";
        public const string GITHUB_repo = "DGJv3";

        /// <summary>
        /// API的路径
        /// </summary>
        public const string API_PATH = "/api/v2/";

        /// <summary>
        /// 插件介绍页的路径
        /// </summary>
        public const string WEB_PATH = "/plugins/";

        #endregion

        #region -- 变量 --
        private string BaseUrl;

        /// <summary>
        /// 最后一个出错的Exception
        /// </summary>
        public Exception LastException { get; private set; } = null;

        /// <summary>
        /// 插件ID
        /// </summary>
        public string Id { get; private set; } = "example";

        /// <summary>
        /// API返回的插件名字
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 更新文件的名称
        /// </summary>
        public string UpdateFileName { get; set; }

        /// <summary>
        /// API返回的插件作者
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// API返回的最新版本号
        /// </summary>
        public Version Version { get; private set; }

        /// <summary>
        /// API返回的插件说明
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// API返回的更新时间
        /// </summary>
        public DateTime UpdateDateTime { get; private set; }

        /// <summary>
        /// API返回的更新说明
        /// </summary>
        public string UpdateDescription { get; private set; }

        /// <summary>
        /// API返回的下载地址
        /// </summary>
        public Uri DownloadUrl { get; private set; }

        /// <summary>
        /// 更新下载页面地址
        /// </summary>
        public Uri UpdatePage { get; private set; }


        /// <summary>
        /// 用户查看用的页面地址
        /// </summary>
        public Uri WebPageUrl
        {
            get
            { return new Uri(this.BaseUrl + WEB_PATH + this.Id); }
        }

        /// <summary>
        /// API返回的下载说明
        /// </summary>
        public string DownloadNote { get; private set; }

        #endregion

        /// <summary>
        /// 新建一个版本检查类
        /// </summary>
        /// <param name="id">插件ID</param>
        public VersionChecker(string id) : this(id, DEFAULT_BASE_URL)
        { }

        /// <summary>
        /// 新建一个指定服务器地址的版本检查类
        /// </summary>
        /// <param name="id">插件ID</param>
        /// <param name="baseurl">服务器地址</param>
        public VersionChecker(string id, string baseurl)
        {
            this.Id = id;
            this.BaseUrl = baseurl;
        }

        /// <summary>
        /// 从服务器获取信息
        /// </summary>
        /// <returns>获取是否成功</returns>
        public bool FetchInfo()
        {
            try
            {
                Uri fullUri = new Uri(BaseUrl + API_PATH + this.Id);
                string json = HttpGet(fullUri);
                JObject j = JObject.Parse(json);

                if (j["id"].ToString() != this.Id)
                { LastException = new Exception("API返回ID不正确"); return false; }
                else
                {
                    this.Name = j["name"].ToString();
                    this.Author = j["author"].ToString();
                    this.Version = new Version(j["version"].ToString());
                    this.Description = j["description"].ToString();

                    //this.UpdateTime = DateTime.ParseExact(j["updatetime"].ToString(), "yyyy.MM.dd", CultureInfo.InvariantCulture);
                    this.UpdateDateTime = DateTimeOffset.Parse(j["update_datetime"].ToString(), null).DateTime;
                    this.UpdateDescription = j["update_desc"].ToString();

                    //this.DownloadUrl = j["dl_url"].ToString().StartsWith("http") ? new Uri(j["dl_url"].ToString()) : new Uri(this.BaseUrl + j["dl_url"].ToString());
                    this.DownloadUrl = new Uri(new Uri(this.BaseUrl), j["dl_url"].ToString());
                    this.DownloadNote = j["dl_note"].ToString();

                    return true;
                }
            }
            catch (Exception ex)
            { LastException = ex; return false; }

        }

        public bool FetchInfoFromGithub()
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36");

                    string url = $"https://api.github.com/repos/{GITHUB_owner}/{GITHUB_repo}/releases/latest";
                    HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        JObject release = JsonConvert.DeserializeObject<JObject>(responseContent);
                        string releaseName = release.GetValue("name").Value<string>();
                        this.Name = "点歌姬v3";
                        this.Author = "Simon";
                        this.Version = new Version(releaseName?.Replace("v", "")?.Replace("V", ""));
                        this.UpdateDateTime = release.GetValue("created_at").Value<DateTime>().ToLocalTime();
                        this.UpdateDescription = release.GetValue("body").Value<string>();
                        this.DownloadNote = "";
                        this.UpdatePage = new Uri(release.GetValue("html_url").Value<string>());

                        var assets = release.GetValue("assets").Value<JArray>();
                        if (assets != null)
                        {
                            foreach (var asset in assets)
                            {
                                if (asset.Value<string>("name") == $"DGJv3_{releaseName?.Replace("v", "")?.Replace("V", "")}.zip")
                                {
                                    this.DownloadUrl = new Uri(asset.Value<string>("browser_download_url"));
                                    this.UpdateFileName = asset.Value<string>("name");
                                }
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            { LastException = ex; return false; }
        }

        /// <summary>
        /// 检查是否具有更新的版本
        /// </summary>
        /// <param name="version">当前版本号</param>
        /// <returns>检查结果</returns>
        public bool HasNewVersion(string version)
        { return HasNewVersion(new Version(version)); }

        /// <summary>
        /// 检查是否具有更新的版本
        /// </summary>
        /// <param name="version">当前版本号</param>
        /// <returns>检查结果</returns>
        public bool HasNewVersion(Version version)
        { return (version.CompareTo(this.Version) < 0); }
        // version对象比参数小（之前，older的版本）


        private string HttpGet(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            { var text = reader.ReadToEnd(); return text; }
        }
    }
}
