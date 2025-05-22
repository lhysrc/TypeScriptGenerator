namespace Runner.Tests

open System
open System.Reflection
open NUnit.Framework
open TypeScriptGenerator
open TypeScriptGenerator.Configuration
open TypeScriptGenerator.DocGenerator
open TypeScriptGenerator.ContentGeneratorHelpers // For prependJsDocIfAny and other helpers
open TypeScriptGenerator.ModelContentGenerator
open TypeScriptGenerator.EnumContentGenerator
open TypeScriptGenerator.TS // For TS.indent

// Helper to normalize line endings for comparisons
let private normalizeLineEndings (text: string) =
    text.Replace("\r\n", "\n").Trim()

[<TestFixture>]
type JSDocGeneratorTests() =

    [<SetUp>]
    member this.Setup() =
        Configuration.xmlDocs.Clear()
        Configuration.currentEnableJsDoc <- false // Default to JSDoc disabled

    // --- FullDocumentation Tests ---
    [<Test>]
    member this.Test_GenerateJsDoc_FullDocumentation_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let xmlKey = "M:MyNamespace.MyClass.MyMethod(System.String,System.Int32)"
        let xmlContent = """
<summary>This is a summary.</summary>
<param name="param1">Description for param1.</param>
<param name="param2">Description for param2.</param>
<returns>Returns a string.</returns>
<remarks>Some remarks here.</remarks>
<typeparam name="T">A generic type parameter.</typeparam>
"""
        Configuration.addXmlDoc xmlKey xmlContent
        let expectedJsDoc = normalizeLineEndings """
/**
 * This is a summary.
 * @template T A generic type parameter.
 * @param param1 Description for param1.
 * @param param2 Description for param2.
 * @returns Returns a string.
 *
 * Some remarks here.
 */
"""
        let actualJsDoc = normalizeLineEndings (DocGenerator.generateJsDoc xmlKey)
        Assert.AreEqual(expectedJsDoc, actualJsDoc)

    [<Test>]
    member this.Test_GenerateJsDoc_FullDocumentation_Disabled() =
        // currentEnableJsDoc is false by default from Setup
        let xmlKey = "M:MyNamespace.MyClass.MyMethod(System.String,System.Int32)"
        let xmlContent = "<summary>This is a summary.</summary>" // Content doesn't matter as much
        Configuration.addXmlDoc xmlKey xmlContent
        let actualJsDoc = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDoc), "JSDoc should be empty when disabled.")

    // --- SummaryOnly Tests ---
    [<Test>]
    member this.Test_GenerateJsDoc_SummaryOnly_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let xmlKey = "T:MyNamespace.MySimpleClass"
        let xmlContent = "<summary>Simple summary.</summary>"
        Configuration.addXmlDoc xmlKey xmlContent
        let expectedJsDoc = normalizeLineEndings """
/**
 * Simple summary.
 */
"""
        let actualJsDoc = normalizeLineEndings (DocGenerator.generateJsDoc xmlKey)
        Assert.AreEqual(expectedJsDoc, actualJsDoc)

    [<Test>]
    member this.Test_GenerateJsDoc_SummaryOnly_Disabled() =
        let xmlKey = "T:MyNamespace.MySimpleClass"
        let xmlContent = "<summary>Simple summary.</summary>"
        Configuration.addXmlDoc xmlKey xmlContent
        let actualJsDoc = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDoc), "JSDoc should be empty when disabled.")

    // --- MultiLineSummary Tests ---
    [<Test>]
    member this.Test_GenerateJsDoc_MultiLineSummary_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let xmlKey = "T:MyNamespace.MyMultiLineClass"
        let xmlContent = "<summary>Line 1.\nLine 2.</summary>"
        Configuration.addXmlDoc xmlKey xmlContent
        let expectedJsDoc = normalizeLineEndings """
/**
 * Line 1.
 * Line 2.
 */
"""
        let actualJsDoc = normalizeLineEndings (DocGenerator.generateJsDoc xmlKey)
        Assert.AreEqual(expectedJsDoc, actualJsDoc)
        
    [<Test>]
    member this.Test_GenerateJsDoc_MultiLineSummary_Disabled() =
        let xmlKey = "T:MyNamespace.MyMultiLineClass"
        let xmlContent = "<summary>Line 1.\nLine 2.</summary>"
        Configuration.addXmlDoc xmlKey xmlContent
        let actualJsDoc = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDoc), "JSDoc should be empty when disabled.")

    // --- EmptyContent, NoMatchingKey Tests (Should always be empty regardless of EnableJsDoc flag) ---
    [<Test>]
    member this.Test_GenerateJsDoc_EmptyContent_RegardlessOfFlag() =
        let xmlKey = "M:MyNamespace.MyClass.DoNothing"
        Configuration.addXmlDoc xmlKey "   " // Whitespace content
        
        Configuration.currentEnableJsDoc <- true // Test with flag true
        let actualJsDocEnabled = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDocEnabled), "JSDoc should be empty for whitespace XML (Enabled).")
        
        Configuration.currentEnableJsDoc <- false // Test with flag false
        let actualJsDocDisabled = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDocDisabled), "JSDoc should be empty for whitespace XML (Disabled).")

    [<Test>]
    member this.Test_GenerateJsDoc_NoMatchingKey_RegardlessOfFlag() =
        Configuration.currentEnableJsDoc <- true // Test with flag true
        let actualJsDocEnabled = DocGenerator.generateJsDoc "M:NonExistent.Key"
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDocEnabled), "JSDoc should be empty for non-existent key (Enabled).")

        Configuration.currentEnableJsDoc <- false // Test with flag false
        let actualJsDocDisabled = DocGenerator.generateJsDoc "M:NonExistent.Key"
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDocDisabled), "JSDoc should be empty for non-existent key (Disabled).")

    // --- ParamAndReturnsOnly Tests ---
    [<Test>]
    member this.Test_GenerateJsDoc_ParamAndReturnsOnly_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let xmlKey = "M:MyNamespace.MyClass.Process(System.Int32)"
        let xmlContent = """
<param name="input">The input integer.</param>
<returns>The processed integer.</returns>
"""
        Configuration.addXmlDoc xmlKey xmlContent
        let expectedJsDoc = normalizeLineEndings """
/**
 * @param input The input integer.
 * @returns The processed integer.
 */
"""
        let actualJsDoc = normalizeLineEndings (DocGenerator.generateJsDoc xmlKey)
        Assert.AreEqual(expectedJsDoc, actualJsDoc)

    [<Test>]
    member this.Test_GenerateJsDoc_ParamAndReturnsOnly_Disabled() =
        let xmlKey = "M:MyNamespace.MyClass.Process(System.Int32)"
        let xmlContent = "<param name=\"input\">The input integer.</param><returns>The processed integer.</returns>"
        Configuration.addXmlDoc xmlKey xmlContent
        let actualJsDoc = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDoc), "JSDoc should be empty when disabled.")

    // --- TypeParamOnly Tests ---
    [<Test>]
    member this.Test_GenerateJsDoc_TypeParamOnly_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let xmlKey = "T:MyNamespace.GenericType`1"
        let xmlContent = "<typeparam name=\"TKey\">The key type.</typeparam>"
        Configuration.addXmlDoc xmlKey xmlContent
        let expectedJsDoc = normalizeLineEndings """
/**
 * @template TKey The key type.
 */
"""
        let actualJsDoc = normalizeLineEndings (DocGenerator.generateJsDoc xmlKey)
        Assert.AreEqual(expectedJsDoc, actualJsDoc)
        
    [<Test>]
    member this.Test_GenerateJsDoc_TypeParamOnly_Disabled() =
        let xmlKey = "T:MyNamespace.GenericType`1"
        let xmlContent = "<typeparam name=\"TKey\">The key type.</typeparam>"
        Configuration.addXmlDoc xmlKey xmlContent
        let actualJsDoc = DocGenerator.generateJsDoc xmlKey
        Assert.IsTrue(String.IsNullOrWhiteSpace(actualJsDoc), "JSDoc should be empty when disabled.")


[<TestFixture>]
type JSDocContentGeneratorTests() =

    [<SetUp>]
    member this.Setup() =
        Configuration.xmlDocs.Clear()
        let opts = ModelGenerateOptions "" // Dummy path for ModelGenerateOptions
        // This will set Configuration.currentEnableJsDoc based on opts.EnableJsDoc (which is false by default)
        Configuration.setOptions opts 
        // Explicitly ensure it's false for tests if there's any doubt about ModelGenerateOptions default
        Configuration.currentEnableJsDoc <- false


    type MyTestClass = {
        [<System.ComponentModel.Description("This is a test property.")>]
        MyProperty: string
    }

    type MyTestEnum =
        | ValueOne = 0
        | ValueTwo = 1

    // --- Property Tests ---
    [<Test>]
    member this.Test_Property_WithJsDoc_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let propInfo = typeof<MyTestClass>.GetProperty("MyProperty")
        let xmlKey = $"P:{propInfo.DeclaringType.FullName}.{propInfo.Name}"
        Configuration.addXmlDoc xmlKey "<summary>This is a property summary.</summary><remarks>With remarks.</remarks>"
        
        let tsImports = System.Collections.Generic.HashSet<Type>()
        let generatedProperty = ModelContentGenerator.generateProperty tsImports propInfo
        
        let expectedOutput = normalizeLineEndings $"""
{TS.indent}/**
{TS.indent} * This is a property summary.
{TS.indent} *
{TS.indent} * With remarks.
{TS.indent} */
{TS.indent}myProperty?: string;
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedProperty)

    [<Test>]
    member this.Test_Property_WithJsDoc_Disabled() =
        // currentEnableJsDoc is false by default from Setup
        let propInfo = typeof<MyTestClass>.GetProperty("MyProperty")
        let xmlKey = $"P:{propInfo.DeclaringType.FullName}.{propInfo.Name}"
        Configuration.addXmlDoc xmlKey "<summary>This is a property summary.</summary>" // XML content added but should not be used
        
        let tsImports = System.Collections.Generic.HashSet<Type>()
        let generatedProperty = ModelContentGenerator.generateProperty tsImports propInfo
        
        let expectedOutput = normalizeLineEndings $"""
{TS.indent}myProperty?: string;
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedProperty, "Property code should be generated without JSDoc when disabled.")
        Assert.IsFalse(generatedProperty.Contains("/**"), "Output should not contain JSDoc start when disabled.")


    // --- Enum Tests ---
    [<Test>]
    member this.Test_Enum_WithJsDoc_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let enumType = typeof<MyTestEnum>
        let enumXmlKey = $"T:{enumType.FullName}"
        let memberOneXmlKey = $"F:{enumType.FullName}.ValueOne"
        let memberTwoXmlKey = $"F:{enumType.FullName}.ValueTwo"

        Configuration.addXmlDoc enumXmlKey "<summary>This is an enum.</summary>"
        Configuration.addXmlDoc memberOneXmlKey "<summary>First enum value.</summary>"
        Configuration.addXmlDoc memberTwoXmlKey "<summary>Second enum value.</summary>"

        let typeOptions = TypeOptions(enumType, "/output/MyTestEnum.ts")
        let (generatedEnumContent, _) = EnumContentGenerator.generateContent typeOptions
        
        let expectedOutput = normalizeLineEndings $"""
/**
 * This is an enum.
 */
export enum MyTestEnum {
{TS.indent}/**
{TS.indent} * First enum value.
{TS.indent} */
{TS.indent}ValueOne = 0,
{TS.indent}/**
{TS.indent} * Second enum value.
{TS.indent} */
{TS.indent}ValueTwo = 1
}
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedEnumContent)

    [<Test>]
    member this.Test_Enum_WithJsDoc_Disabled() =
        let enumType = typeof<MyTestEnum>
        let enumXmlKey = $"T:{enumType.FullName}"
        let memberOneXmlKey = $"F:{enumType.FullName}.ValueOne"
        // Add XML doc, it shouldn't be used
        Configuration.addXmlDoc enumXmlKey "<summary>This is an enum.</summary>"
        Configuration.addXmlDoc memberOneXmlKey "<summary>First enum value.</summary>"

        let typeOptions = TypeOptions(enumType, "/output/MyTestEnum.ts")
        let (generatedEnumContent, _) = EnumContentGenerator.generateContent typeOptions
        
        let expectedOutput = normalizeLineEndings $"""
export enum MyTestEnum {
{TS.indent}ValueOne = 0,
{TS.indent}ValueTwo = 1
}
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedEnumContent, "Enum code should be generated without JSDoc when disabled.")
        Assert.IsFalse(generatedEnumContent.Contains("/**"), "Output should not contain JSDoc start when disabled.")

    // --- Class Tests ---
    [<Test>]
    member this.Test_Class_WithJsDoc_Enabled() =
        Configuration.currentEnableJsDoc <- true
        let classType = typeof<MyTestClass>
        let classXmlKey = $"T:{classType.FullName}"
        let propXmlKey = $"P:{classType.FullName}.MyProperty"

        Configuration.addXmlDoc classXmlKey "<summary>This is a test class.</summary><typeparam name=\"T\">A type param for class</typeparam>"
        Configuration.addXmlDoc propXmlKey "<summary>Property in test class.</summary>"

        let typeOptions = TypeOptions(classType, "/output/MyTestClass.ts")
        let (generatedClassContent, _) = ModelContentGenerator.generateContent typeOptions
        
        let expectedOutput = normalizeLineEndings $"""
/**
 * This is a test class.
 * @template T A type param for class
 */
export class MyTestClass {
{TS.indent}/**
{TS.indent} * Property in test class.
{TS.indent} */
{TS.indent}myProperty?: string;
}
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedClassContent)

    [<Test>]
    member this.Test_Class_WithJsDoc_Disabled() =
        let classType = typeof<MyTestClass>
        let classXmlKey = $"T:{classType.FullName}"
        let propXmlKey = $"P:{classType.FullName}.MyProperty"
        // Add XML doc, it shouldn't be used
        Configuration.addXmlDoc classXmlKey "<summary>This is a test class.</summary>"
        Configuration.addXmlDoc propXmlKey "<summary>Property in test class.</summary>"

        let typeOptions = TypeOptions(classType, "/output/MyTestClass.ts")
        let (generatedClassContent, _) = ModelContentGenerator.generateContent typeOptions
        
        let expectedOutput = normalizeLineEndings $"""
export class MyTestClass {
{TS.indent}myProperty?: string;
}
"""
        Assert.AreEqual(expectedOutput, normalizeLineEndings generatedClassContent, "Class code should be generated without JSDoc when disabled.")
        Assert.IsFalse(generatedClassContent.Contains("/**"), "Output should not contain JSDoc start when disabled.")
