using DocumentFormat.OpenXml.Drawing.Diagrams;
using Newtonsoft.Json;
using Openize.Words;
using Openize.Words.IElements;
using WordCreationTester;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.Identity;
using OpenAI.Chat;
using static System.Environment;
using Microsoft.VisualBasic;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Charts;

runAI();

static void runAI()
{

    string AI_endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://<your-resource-name>.openai.azure.com/";
    string AI_key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<your-key>";
    string searchEndpoint = GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT");
    string searchKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY") ?? "<your-search-api-key>";


    var searchIndexes = new List<string>
        {
            "gptindex",
            "incident-index",
        };

    var responses = new List<string> { };

    foreach (String index in searchIndexes)
    {
        try
        {
            // Use the recommended keyless credential instead of the AzureKeyCredential credential.
            // AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
            AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(AI_endpoint), new AzureKeyCredential(AI_key));

            // This must match the custom deployment name you chose for your model
            ChatClient chatClient = openAIClient.GetChatClient("gpt-4o-mini");

            ChatCompletionOptions options = new ChatCompletionOptions();

#pragma warning disable AOAI001 // Suppress the diagnostic warning  

            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(searchEndpoint),
                IndexName = index,
                Authentication = DataSourceAuthentication.FromApiKey(searchKey), // Add your Azure AI Search admin key here  
            });

            Console.WriteLine(options.GetDataSources());

            ChatCompletion completion = chatClient.CompleteChat([
                new SystemChatMessage("You are a helpful assistant that collects information that we request by looking at the provided datasources. If you find nothing of relevance in the datasource, just return an empty string and nothing else. This is fine and shouldn't be seen as an error."),
                new UserChatMessage("Show all incidents involving luke")
            ], options);

            responses.Add(completion.Content[0].Text);
        }
        catch (Exception e)
        {
            Console.WriteLine("nothing found here.");
        }
    }

    foreach (string res in responses)
    {
        Console.WriteLine(res);
    }
}

static void runGeneration()
{
    string docsDirectory = "./docs";
    string filename = "Generated.docx";

    // JSON string representing the report data
    string jsonString = @"{
          ""reportTitle"": ""The Humble Dandelion: More Than Just a Weed"",
          ""sections"": [
            {
              ""sectionTitle"": ""Introduction"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
              ""sections"": []
            },
            {
              ""sectionTitle"": ""Taxonomy and Nomenclature"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""The name 'dandelion' is derived from the French 'dent de lion.'"",
              ""sections"": []
            },
            {
              ""sectionTitle"": ""Morphology and Life Cycle"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""The dandelion is a perennial plant."",
              ""sections"": [
                {
                  ""sectionTitle"": ""Roots"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions possess a strong taproot."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Leaves"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""The leaves are lanceolate."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Scape"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""The scape is a smooth stalk."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Flower Head"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""The flower head is a composite structure."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Seed Head"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""The flower head transforms into a seed head."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Life Cycle Description"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""The dandelion life cycle begins with germination."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Ecological Role"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions play a role in ecosystems."",
              ""sections"": [
                {
                  ""sectionTitle"": ""Weed Considerations"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions are considered weeds."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Historical and Ethnobotanical Uses"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions have been used for medicinal purposes."",
              ""sections"": [
                {
                  ""sectionTitle"": ""European Uses"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions have been a food source in Europe."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Nutritional Value"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions are surprisingly nutritious."",
              ""sections"": [
                {
                  ""sectionTitle"": ""Flower and Root Notes"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Flowers and roots contain beneficial compounds."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Nutritional Profile"",
                  ""sectionContext"": ""table"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Detailed nutritional information."",
                  ""sections"": [],
                  ""tableData"": {
                    ""rowCount"": 3,
                    ""columnCount"": 3,
                    ""cells"": [
                      { ""content"": ""Nutrient"", ""row"": 0, ""column"": 0, ""fontWeight"": ""bold"" },
                      { ""content"": ""Amount"", ""row"": 0, ""column"": 1, ""fontWeight"": ""bold"" },
                      { ""content"": ""Unit"", ""row"": 0, ""column"": 2, ""fontWeight"": ""bold"" },
                      { ""content"": ""Calories"", ""row"": 1, ""column"": 0, ""fontWeight"": ""normal"" },
                      { ""content"": ""25"", ""row"": 1, ""column"": 1, ""fontWeight"": ""normal"" },
                      { ""content"": null, ""row"": 1, ""column"": 2, ""fontWeight"": ""normal"" },
                      { ""content"": ""Carbohydrates"", ""row"": 2, ""column"": 0, ""fontWeight"": ""normal"" },
                      { ""content"": ""5.1"", ""row"": 2, ""column"": 1, ""fontWeight"": ""normal"" },
                      { ""content"": ""g"", ""row"": 2, ""column"": 2, ""fontWeight"": ""normal"" }
                    ],
                    ""caption"": ""1 cup raw dandelion greens""
                  }
                }
              ]
            },
            {
              ""sectionTitle"": ""Potential Health Benefits"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions have health benefits:"",
              ""sections"": [
                {
                  ""sectionTitle"": ""Liver Health"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions protect the liver."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Digestive Health"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Fiber promotes digestion."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Diuretic Effect"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions have a diuretic effect."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Blood Sugar Control"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions may improve sugar control."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Anti-inflammatory Effects"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions contain anti-inflammatory compounds."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Antioxidant Activity"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions have antioxidant activity."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Disclaimer"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions are not a substitute for treatment."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Culinary Uses"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions are used in cooking:"",
              ""sections"": [
                {
                  ""sectionTitle"": ""Dandelion Salad"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Recipe for Dandelion Salad."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Sautéed Dandelion Greens"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Recipe for Sautéed Dandelion Greens."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Dandelion Tea"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Recipe for Dandelion Tea."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Other Uses"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions have other uses:"",
              ""sections"": [
                {
                  ""sectionTitle"": ""Natural Dye"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions make a natural dye."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Rubber Production"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions contain latex for rubber."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Soil Remediation"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions are used for remediation."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Animal Feed"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Dandelions are used as animal feed."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Other Uses"",
              ""sectionContext"": ""dotpoint"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions have other uses:"",
              ""sections"": [
                {
                  ""sectionTitle"": ""Natural Dye"",
                  ""sectionContext"": ""dotpoint"",
                  ""paragraphContext"": ""dotpoint"",
                  ""content"": ""Dandelions make a natural dye."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Rubber Production"",
                  ""sectionContext"": ""dotpoint"",
                  ""paragraphContext"": ""dotpoint"",
                  ""content"": ""Dandelions contain latex for rubber."",
                  ""sections"": [
                    {
                        ""sectionTitle"": ""Natural Dye"",
                        ""sectionContext"": ""dotpoint"",
                        ""paragraphContext"": ""dotpoint"",
                        ""content"": ""Dandelions make a natural dye."",
                        ""sections"": []
                    },
                    {
                        ""sectionTitle"": ""Rubber Production"",
                        ""sectionContext"": ""dotpoint"",
                        ""paragraphContext"": ""dotpoint"",
                        ""content"": ""Dandelions contain latex for rubber."",
                        ""sections"": [
                        {
                            ""sectionTitle"": ""Natural Dye"",
                            ""sectionContext"": ""dotpoint"",
                            ""paragraphContext"": ""dotpoint"",
                            ""content"": ""Dandelions make a natural dye."",
                            ""sections"": []
                        },
                        {
                            ""sectionTitle"": ""Rubber Production"",
                            ""sectionContext"": ""dotpoint"",
                            ""paragraphContext"": ""dotpoint"",
                            ""content"": ""Dandelions contain latex for rubber."",
                            ""sections"": []
                        }
                      ]
                    }
                  ]
                },
                {
                  ""sectionTitle"": ""Soil Remediation"",
                  ""sectionContext"": ""dotpoint"",
                  ""paragraphContext"": ""dotpoint"",
                  ""content"": ""Dandelions are used for remediation."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Animal Feed"",
                  ""sectionContext"": ""dotpoint"",
                  ""paragraphContext"": ""dotpoint"",
                  ""content"": ""Dandelions are used as animal feed."",
                  ""sections"": []
                }
              ]
            },
{
              ""sectionTitle"": ""Other Uses"",
              ""sectionContext"": ""numberList"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions have other uses:"",
              ""sections"": [
                {
                  ""sectionTitle"": ""Natural Dye"",
                  ""sectionContext"": ""numberList"",
                  ""paragraphContext"": ""numberList"",
                  ""content"": ""Dandelions make a natural dye."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Rubber Production"",
                  ""sectionContext"": ""numberList"",
                  ""paragraphContext"": ""numberList"",
                  ""content"": ""Dandelions contain latex for rubber."",
                  ""sections"": [
                    {
                        ""sectionTitle"": ""Natural Dye"",
                        ""sectionContext"": ""numberList"",
                        ""paragraphContext"": ""numberList"",
                        ""content"": ""Dandelions make a natural dye."",
                        ""sections"": []
                    },
                    {
                        ""sectionTitle"": ""Rubber Production"",
                        ""sectionContext"": ""numberList"",
                        ""paragraphContext"": ""numberList"",
                        ""content"": ""Dandelions contain latex for rubber."",
                        ""sections"": [
                        {
                            ""sectionTitle"": ""Natural Dye"",
                            ""sectionContext"": ""numberList"",
                            ""paragraphContext"": ""numberList"",
                            ""content"": ""Dandelions make a natural dye."",
                            ""sections"": []
                        },
                        {
                            ""sectionTitle"": ""Rubber Production"",
                            ""sectionContext"": ""numberList"",
                            ""paragraphContext"": ""numberList"",
                            ""content"": ""Dandelions contain latex for rubber."",
                            ""sections"": []
                        }
                      ]
                    }
                  ]
                },
                {
                  ""sectionTitle"": ""Soil Remediation"",
                  ""sectionContext"": ""numberList"",
                  ""paragraphContext"": ""numberList"",
                  ""content"": ""Dandelions are used for remediation."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Animal Feed"",
                  ""sectionContext"": ""numberList"",
                  ""paragraphContext"": ""numberList"",
                  ""content"": ""Dandelions are used as animal feed."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Challenges and Future Directions"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""Dandelions are a nuisance."",
              ""sections"": [
                {
                  ""sectionTitle"": ""Variability in quality"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Quality is a challenge."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Lack of awareness"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Awareness is lacking."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Optimizing cultivation"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Optimizing cultivation is a future direction."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Identifying bioactive compounds"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Identifying compounds is a future direction."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Evaluating health benefits"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Evaluating benefits is a future direction."",
                  ""sections"": []
                },
                {
                  ""sectionTitle"": ""Developing new products"",
                  ""sectionContext"": ""paragraph"",
                  ""paragraphContext"": ""normal"",
                  ""content"": ""Developing products is a future direction."",
                  ""sections"": []
                }
              ]
            },
            {
              ""sectionTitle"": ""Conclusion"",
              ""sectionContext"": ""paragraph"",
              ""paragraphContext"": ""normal"",
              ""content"": ""The dandelion is a plant with potential."",
              ""sections"": []
            }
          ]
        }";

    try
    {
        // Deserialize the JSON string into a Report object
        Report report = JsonConvert.DeserializeObject<Report>(jsonString);

        var doc = new Document();

        var word = new WordFileGenerator(doc);

        word.addTitle(report.ReportTitle);

        foreach (Section s in report.Sections)
        {
            ParseSection(word, s);
        }

        doc.Save($"{docsDirectory}/{filename}");
    }
    catch (System.Exception ex)
    {
        throw new FileFormatException("An error occurred.", ex);
    }

    static void ParseSection(WordFileGenerator w, Section s)
    {
        if (s.SectionTitle != "")
        {
            w.addHeader(s.SectionTitle);
        }

        if (s.SectionContext.Equals("paragraph"))
        {
            parseNormalSection(w, s);
        }
        else if (s.SectionContext.Equals("table"))
        {
            parseTable(w, s);
        }
        else if (s.SectionContext.Equals("dotpoint"))
        {
            parseDotPoints(w, s);
        }
        else if (s.SectionContext.Equals("numberList"))
        {
            parseNumberList(w, s);
        }

        foreach (Section subSection in s.Sections)
        {
            ParseSection(w, subSection);
        }

    }

}

static void parseNormalSection(WordFileGenerator w, Section s)
{
    if (s.ParagraphContext.Equals("normal") && s.Content != "")
    {
        w.addParagraph(s.Content);
    }
}

static void parseTable(WordFileGenerator w, Section s)
{
    if (s.TableData != null)
    {
        w.addTable(s.TableData.RowCount, s.TableData.ColumnCount);

        foreach (Cell c in s.TableData.Cells)
        {

            w.addTableCell(c.Content, c.FontWeight, c.Row, c.Column);
        }

        if (!s.TableData.Caption.Equals(""))
        {
            w.addParagraph(s.TableData.Caption);
        }
    }
}

static void parseDotPoints(WordFileGenerator w, Section s, int indent = 1)
{
    if (s.ParagraphContext.Equals("paragraph"))
    {
        w.addParagraph(s.Content);
    }
    else if (s.ParagraphContext.Equals("dotpoint"))
    {
        w.addDotpointParagraph(s.Content, indent);
    }

    foreach (Section subSection in s.Sections)
    {
        parseDotPoints(w, subSection, indent + 1);
    }
}

static void parseNumberList(WordFileGenerator w, Section s, int indent = 1, int number = 1)
{
    if (s.ParagraphContext.Equals("paragraph"))
    {
        w.addParagraph(s.Content);
    }
    else if (s.ParagraphContext.Equals("numberList"))
    {
        w.addNumericListParagraph(s.Content, number, indent);
    }

    int subNumber = 1;

    foreach (Section subSection in s.Sections)
    {
        parseNumberList(w, subSection, indent + 1, subNumber++);
    }
}

//try
//{
//    // Initialize a new word document with the default template         
//    var doc = new Document();
//    System.Console.WriteLine("Word Document with default template initialized");

//    // Initialize the body with the new document
//    var body = new Body(doc);
//    System.Console.WriteLine("Body of the Word Document initialized");

//    var para1 = new Paragraph();

//    para1.Style = Headings.Heading1;

//    para1.AddRun(new Run { 
//        Text = "The Humble Dandelion: More than Just a Weed" 
//    });

//    body.AppendChild(para1);

//    var para2 = new Paragraph();

//    para2.AddRun(new Run
//    {
//        Text = "The dandelion, often dismissed as a common weed, is a plant with a rich history, remarkable adaptability, and surprising benefits. Belonging to the genus Taraxacum, the most well-known species is Taraxacum officinale. This seemingly ubiquitous plant has followed humans across the globe, establishing itself in diverse environments and cultures. This report delves into the dandelion's taxonomy, morphology, ecological role, historical uses, nutritional value, and potential future applications, aiming to provide a comprehensive understanding of this often-underappreciated plant."
//    });

//    body.AppendChild(para2);

//    var para3 = new Paragraph();

//    para3.Style = Headings.Heading2;

//    para3.AddRun(new Run
//    {
//        Text = "Taxonomy and Nomenclature"
//    });

//    body.AppendChild(para3);

//    var para4 = new Paragraph();

//    para4.AddRun(new Run
//    {
//        Text = "The name \"dandelion\" is derived from the French \"dent de lion,\" meaning \"lion's tooth,\" referring to the plant's sharply toothed leaves. The genus Taraxacum is part of the Asteraceae family, also known as the sunflower or daisy family, a large and diverse group of flowering plants. Taraxacum officinale is considered an apomictic species, meaning it can reproduce asexually through seeds that develop without fertilization. This contributes to its widespread distribution and ability to colonize new areas rapidly.\r\n\r\n"
//    });

//    body.AppendChild(para4);

//    var para5 = new Paragraph();

//    para5.Style = Headings.Heading2;

//    para5.AddRun(new Run
//    {
//        Text = "Morphology and Life Cycle"
//    });

//    body.AppendChild(para5);

//    var para6 = new Paragraph();
//    para6.Style = "ListParagraph";

//    para6.IsBullet = true;
//    para6.NumberingId = 1; // This is required for bullets
//    para6.NumberingLevel = 1; // This controls indents

//    para6.AddRun(new Run
//    {
//        Text = "Roots: Dandelions possess a strong taproot, which can grow quite deep into the soil, allowing the plant to access water and nutrients even in relatively dry conditions. This taproot also serves as a storage organ, enabling the plant to survive through winter and regenerate in the spring.\n\r"
//    });


//    body.AppendChild(para6);

//    var para7 = new Paragraph();

//    para7.Style = Headings.Heading1;

//    para7.AddRun(new Run
//    {
//        Text = "Nutritional Value\r\n"
//    });

//    body.AppendChild(para7);

//    var table1 = new Table(5, 5);

//    int rowNumber = 0;
//    int columnNumber = 0;

//    table1.Style = doc.GetElementStyles().TableStyles[1];

//    foreach (var row in table1.Rows)
//    {
//        rowNumber++;
//        foreach (var cell in row.Cells)
//        {
//            columnNumber++;
//            var para = new Paragraph();
//            para.AddRun(new Run
//            {
//                Text = $"Row {rowNumber} Column {columnNumber}"
//            });
//            cell.Paragraphs.Add(para);
//        }
//        columnNumber = 0;
//    }

//    body.AppendChild(table1);

//    // Save the newly created Word Document.
//    doc.Save($"{docsDirectory}/{filename}");
//}
//catch (System.Exception ex)
//{
//    throw new FileFormatException("An error occurred.", ex);
//}