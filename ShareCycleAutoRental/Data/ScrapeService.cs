using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShareCycleAutoRental.Data
{
    public class ScrapeService
    {
        private static int ScrapeSpan = 2000;

        public async Task<Result<BikeInfo>> ScrapeShareAsync(LoginInfo loginInfo, ScrapeCondition scrapeCondition)
        {
            Console.WriteLine(loginInfo.Password);
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            var queryDocument = await context.OpenAsync("https://tcc.docomo-cycle.jp/cycle/TYO/cs_web_main.php");
            var bikeInfos = new List<BikeInfo>();

            //ログイン
            var loginForm = queryDocument.QuerySelector("form") as IHtmlFormElement;
            var resultDocument = await loginForm.SubmitAsync(new { MemberID = loginInfo.UserID, Password = loginInfo.Password });
            {
                var userinfo = resultDocument.GetElementsByClassName("user_inf pc_view").FirstOrDefault();
                if (userinfo == null)
                {
                    var loginError = resultDocument.GetElementsByClassName("err_text").FirstOrDefault();
                    return new Result<BikeInfo>()
                    {
                        HasError = true,
                        Message = loginError?.TextContent ?? "ログインに失敗しました。",
                        ResultObject = null,
                    };
                }
            }

            //Wait
            System.Threading.Thread.Sleep(ScrapeSpan);

            //駐輪場から選ぶへ
            var selectByPlaceForm = resultDocument.GetElementsByName("from_port_tab").FirstOrDefault() as IHtmlFormElement;
            var portDocument = await selectByPlaceForm.SubmitAsync();
            var portTitle = portDocument.GetElementsByClassName("tittle_h1").FirstOrDefault();

            //Wait
            System.Threading.Thread.Sleep(ScrapeSpan);

            //地域を選ぶ
            var searchAreaResult = await SearchCyclePage("sel_area", "AreaID", scrapeCondition.Area, portDocument);
            if (searchAreaResult.HasError)
            {
                return new Result<BikeInfo>()
                {
                    HasError = true,
                    Message = searchAreaResult.Message,
                    ResultObject =  null,
                };
            }

            //Wait
            System.Threading.Thread.Sleep(ScrapeSpan);

            //場所を選ぶ
            var searchPlaceResult = await SearchCyclePage("sel_location", "Location", scrapeCondition.Place, searchAreaResult.ResultObject);
            if (searchPlaceResult.HasError)
            {
                return new Result<BikeInfo>()
                {
                    HasError = true,
                    Message = searchPlaceResult.Message,
                    ResultObject = null,
                };
            }

            //駐輪一覧取得
            var portMain = searchPlaceResult.ResultObject.GetElementsByClassName("main_inner_wide").FirstOrDefault();
            var portList = portMain.GetElementsByClassName("sp_view").FirstOrDefault().QuerySelectorAll("form");
            CyclePortInfo cyclePortInfo = null;
            {
                foreach (var p in portList)
                {
                    var portAtag = p.GetElementsByTagName("a").FirstOrDefault();
                    var splitedPort = portAtag.InnerHtml.Split(new string[] { "< br >", "<br>", "<br/>", "<br />", "< br>", "<br >" }, StringSplitOptions.None);

                    //ポート名で検索
                    if (splitedPort[0].Contains(scrapeCondition.Port))
                    {
                        cyclePortInfo = new CyclePortInfo() { PortName = splitedPort[0], PortNameEnglish = splitedPort[1], PortQuantity = splitedPort[2], Element = p as IHtmlFormElement };
                        break;
                    }
                }
            }
            if (cyclePortInfo == null)
            {
                return new Result<BikeInfo>()
                {
                    HasError = true,
                    Message = "駐輪場の取得に失敗しました",
                    ResultObject = null,
                };
            }

            //Wait
            System.Threading.Thread.Sleep(ScrapeSpan);

            //バイク一覧取得
            var bikePageDocument = await cyclePortInfo.Element.SubmitAsync();
            var bikeForms = bikePageDocument.GetElementsByClassName("main_inner_wide_left_list").FirstOrDefault().GetElementsByTagName("form");

            //Wait
            System.Threading.Thread.Sleep(ScrapeSpan);

            //バイクレンタル main_inner_wide
            var bikeForm = bikeForms.FirstOrDefault() as IHtmlFormElement;
            var bikeInfo = new BikeInfo() { BikeName = bikeForm.TextContent, Element = bikeForm };
            var bikeResultDocument = await bikeForm.SubmitAsync();

            return new Result<BikeInfo>()
            {
                Message = bikeResultDocument.TextContent,
                ResultObject = bikeInfo,
            };
        }

        private static async Task<Result<IDocument>> SearchCyclePage(string formName, string selID, string searchCondition, IDocument portDocument)
        {
            try
            {
                var submit = portDocument.GetElementsByName(formName).FirstOrDefault() as IHtmlFormElement;
                var select = portDocument.GetElementById(selID) as IHtmlSelectElement;
                var option = select.Options.Where(x => x.Text.Contains(searchCondition)).FirstOrDefault();
                option.IsSelected = true;
                var resultDocument = await submit.SubmitAsync();

                return new Result<IDocument>()
                {
                    HasError = false,
                    Message = "",
                    ResultObject = resultDocument,
                };
            }
            catch (Exception ex)
            {
                return new Result<IDocument>()
                {
                    HasError = false,
                    Message = $"{searchCondition}の検索に、失敗しました",
                    ResultObject = null,
                };
            }
        }
    }
}
