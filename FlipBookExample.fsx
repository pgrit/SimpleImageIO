// This F# script file demonstrates how the FlipBook tool can be used in a script to generate a static
// HTML page for image comparisons. See Example.dib for the C# syntax.

// #r "nuget: SimpleImageIO"
#r "./SimpleImageIO/bin/Debug/net7.0/SimpleImageIO.dll"
open SimpleImageIO

let mutable html = "<!DOCTYPE html><html>"

// Create the header code
html <- html + "<head>" + FlipBook.Header + "</head>"
html <- html + "<body>"

// Make some dummy images for testing
let red = new RgbImage(600, 400)
red.Fill(0.3f, 0.03f, 0.05f)
let blue = new RgbImage(600, 400)
blue.Fill(0.03f, 0.3f, 0.7f)
let gray = new MonochromeImage(600, 400)
gray.Fill(0.5f)

// A simple comparison of two RgbImage
html <- html + FlipBook.New.Add("Blue", blue).Add("Red", red).ToString()

// The same, but with a tone mapping function applied to all images
html <- html + FlipBook.New.Add("Blue", blue).Add("Red", red).WithToneMapper(FlipBook.InitialTMO.Exposure(2.0f)).ToString()

// Multiple types of images: cast to base class so we can put them in a list
html <- html + FlipBook.New.Add("Blue", blue).Add("Red", red).Add("Gray", gray).ToString()

html <- html + "</body>"
System.IO.File.WriteAllText("example.html", html)