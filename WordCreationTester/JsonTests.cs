using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCreationTester
{
    public static class JsonTests
    {
        // JSON string representing the report data
        public static string jsonString1 = @"{
""reportTitle"": ""The Humble Dandelion: More Than Just a Weed"",
""sections"": [
{
    ""sectionTitle"": ""Introduction"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
},
{
    ""sectionTitle"": ""Taxonomy and Nomenclature"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The name 'dandelion' is derived from the French 'dent de lion.'The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
},
{
    ""sectionTitle"": ""Morphology and Life Cycle"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The dandelion is a perennial plant. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": [
    {
        ""sectionTitle"": ""Roots"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""Dandelions possess a strong taproot. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
    {
        ""sectionTitle"": ""Leaves"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""The leaves are lanceolate. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
    {
        ""sectionTitle"": ""Scape"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""The scape is a smooth stalk. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
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
        ]
    },
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
    ""sectionTitle"": ""Conclusion"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"", 
    ""content"": ""The dandelion is a plant with potential. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
}
]
}";


        // JSON string representing the report data
        public static string jsonString2 = @"{
""reportTitle"": ""The Humble Dandelion: More Than Just a Weed"",
""sections"": [
{
    ""sectionTitle"": ""Introduction"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
},
{
    ""sectionTitle"": ""Taxonomy and Nomenclature"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The name 'dandelion' is derived from the French 'dent de lion.'The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications.The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
},
{
    ""sectionTitle"": ""Morphology and Life Cycle"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"",
    ""content"": ""The dandelion is a perennial plant. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": [
    {
        ""sectionTitle"": ""Roots"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""Dandelions possess a strong taproot. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
    {
        ""sectionTitle"": ""Leaves"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""The leaves are lanceolate. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
    {
        ""sectionTitle"": ""Scape"",
        ""sectionContext"": ""paragraph"",
        ""paragraphContext"": ""normal"",
        ""content"": ""The scape is a smooth stalk. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
        ""sections"": []
    },
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
        ]
    },
    ]
},
{
    ""sectionTitle"": ""Conclusion"",
    ""sectionContext"": ""paragraph"",
    ""paragraphContext"": ""normal"", 
    ""content"": ""The dandelion is a plant with potential. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications. The dandelion, often dismissed as a common weed, is a plant with a rich history.  This report delves into the dandelion's applications."",
    ""sections"": []
}
]
}";

    }
}
