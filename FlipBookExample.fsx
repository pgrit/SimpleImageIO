// This F# script file demonstrates how the FlipBook tool can be used in a script to generate a static
// HTML page for image comparisons. See Example.dib for the C# syntax.

#r "../bin/Debug/net6.0/SimpleImageIO.dll"
open SimpleImageIO
open SimpleImageIO.FlipBook

let mutable html = "<!DOCTYPE html><html>"

// Create the header code
html <- html + "<head>" + FlipBook.MakeHeader() + "</head>"
html <- html + "<body>"

// Make some dummy images for testing
let red = new RgbImage(600, 400)
red.Fill(0.3f, 0.03f, 0.05f)
let blue = new RgbImage(600, 400)
blue.Fill(0.03f, 0.3f, 0.7f)
let gray = new MonochromeImage(600, 400)
gray.Fill(0.5f)

// A simple comparison of two RgbImage
html <- html + FlipBook.Make [
    "Blue", blue
    "Red", red
]

// The same, but with a tone mapping function applied to all images
let toneMap (img:ImageBase) =
    let cpy = img.Copy()
    cpy.Scale(2.0f**(-1.0f))
    cpy

html <- html + FlipBook.Make (
    toneMap,
    [
        "Blue", blue
        "Red", red
    ]
)

// Multiple types of images: cast to base class so we can put them in a list
html <- html + FlipBook.Make [
    "Blue", blue :> ImageBase
    "Red", red :> ImageBase
    "Gray", gray :> ImageBase
]

// Alternative: Builder class with fluent API for incremental construction
html <- html + FlipBook.New.Add("Blue", blue).Add("Red", red).ToString()

html <- html + "</body>"
System.IO.File.WriteAllText("example.html", html)