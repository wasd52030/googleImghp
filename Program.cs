using System.CommandLine;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

async Task<IEnumerable<IWebElement>> scanImage(ChromeDriver driver, string keyword)
{

    driver.Navigate().GoToUrl("https://www.google.com/imghp?hl=zh-TW");

    var search_box = driver.FindElement(By.XPath("/html/body/div[1]/div[3]/form/div[1]/div[1]/div[1]/div/div[2]/textarea"));
    search_box.SendKeys(keyword);
    search_box.Submit();

    var last_height = driver.ExecuteScript("return document.body.scrollHeight");
    while (true)
    {
        driver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        await Task.Delay(1000);

        var new_height = driver.ExecuteScript("return document.body.scrollHeight");

        if (new_height.Equals(last_height))
        {
            break;
        }

        last_height = new_height;
    }

    var result = driver.FindElements(By.CssSelector("div.H8Rx8c g-img.mNsIhb img"));

    return result;
}

async Task saveImage(ChromeDriver driver, IEnumerable<IWebElement> images, string keyword)
{
    if (!Directory.Exists("images")) Directory.CreateDirectory("images");
    using var client = new HttpClient();
    foreach (var (image, index) in images.Select((image, index) => (image, index)))
    {
        var src = image.GetAttribute("src");


        if (src != null)
        {
            var regex = Regex.Match(src, @"data:image/(?<type>.+?),(?<data>.+)");
            var base64Data = regex.Groups["data"].Value;
            var filetype = regex.Groups[1].Value.Split(";")[0];
            var binData = Convert.FromBase64String(base64Data);

            if (regex.Success)
            {
                if (filetype == "gif") continue;
                await File.WriteAllBytesAsync($"images/{keyword}{index + 1}.{filetype}", binData);
            }
            else
            {
                var response = await client.GetAsync(src);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var file = new FileStream($"images/{keyword}{index + 1}.png", FileMode.Create);
                stream.CopyTo(file);
            }
        }
    }
}

async Task main()
{
    var rootCommand = new RootCommand("簡易 google 圖片爬蟲");

    var keywordOption = new Option<string>
        (aliases: new string[] { "--keyword", "--kw" },
        description: "the keyword you want search",
        getDefaultValue: () => "never gonna give you up");
    rootCommand.AddOption(keywordOption);

    rootCommand.SetHandler(async (keyrowd) =>
    {
        using var driver = new ChromeDriver();

        var images = await scanImage(driver, keyrowd);
        await saveImage(driver, images, keyrowd);
    }, keywordOption);


    await rootCommand.InvokeAsync(args);
}

await main();