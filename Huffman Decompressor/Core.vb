'This module's imports and settings.
Option Compare Binary
Option Explicit On
Option Infer Off
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Convert
Imports System.Environment
Imports System.IO
Imports System.Linq

'This module contains this program's core procedures.
Public Module CoreModule

   Private Const LOOK_UP_TREE_SIZE As Integer = &H1FE%                  'Defines the look up tree's size.
   Private Const LOOK_UP_TREE_TAG As Integer = &H100%                   'Defines the look up tree tag.
   Private Const ROOT As Integer = &H1FC%                               'Defines the look up tree's root.
   Private ReadOnly SIGNATURE() As Byte = {&H48%, &H55%, &H46%, &H46%}  'Defines the "HUFF" signature.

   'This procedure is executed when this program is started.
   Public Sub Main()
      Try
         Dim CompressedData As New Queue(Of Byte)
         Dim DecompressedData As List(Of Byte) = Nothing
         Dim DecompressedSize As New Integer
         Dim InputFile As String = GetCommandLineArgs().Last
         Dim LookUpTree(&H0% To LOOK_UP_TREE_SIZE - &H1%) As Integer
         Dim OutputFile As String = Path.Combine(Path.GetDirectoryName(GetCommandLineArgs().Last), Path.GetFileNameWithoutExtension(GetCommandLineArgs().Last))

         If GetCommandLineArgs().Count = 2 Then
            Console.WriteLine($"Decompressing {InputFile}...")

            Using CompressedFile As New BinaryReader(File.Open(InputFile, FileMode.Open, FileAccess.Read))
               With CompressedFile
                  If .ReadBytes(SIGNATURE.Length).SequenceEqual(SIGNATURE) Then
                     DecompressedSize = .ReadInt32()

                     For Index As Integer = LookUpTree.GetLowerBound(0) To LookUpTree.GetUpperBound(0)
                        LookUpTree(Index) = .ReadUInt16()
                     Next Index

                     Array.ForEach(.ReadBytes(CInt(.BaseStream.Length - .BaseStream.Position)), AddressOf CompressedData.Enqueue)
                  Else
                     Console.WriteLine("The specified file is not a Huffman compressed file.")
                  End If
               End With
            End Using

            If CompressedData.Count > 0 Then
               Console.WriteLine($"Compressed size: {CompressedData.Count}")
               Console.WriteLine($"Decompressed size: {DecompressedSize}")

               DecompressedData = Decompress(CompressedData, LookUpTree, DecompressedSize)

               If DecompressedData IsNot Nothing AndAlso DecompressedData.Count = DecompressedSize - 1 Then
                  File.WriteAllBytes(OutputFile, DecompressedData.ToArray())
                  Console.WriteLine("Done.")
               Else
                  Console.WriteLine("Error decompressing file.")
               End If
            Else
               Console.WriteLine($"Syntax: {GetCommandLineArgs().First} <HUFF file>")
            End If
         Else
            Console.WriteLine("No Huffman compressed data to decompress.")
         End If
      Catch ExceptionO As Exception
         Console.WriteLine($"Error: {ExceptionO.Message}")
      End Try
   End Sub

   'This procedure decompresses the specified data and returns the result.
   Private Function Decompress(CompressedData As Queue(Of Byte), LookUpTree() As Integer, DecompressedSize As Integer) As List(Of Byte)
      Try
         Dim Bit As Integer = &H1%
         Dim ByteO As New Byte
         Dim DecompressedData As New List(Of Byte)
         Dim Leaf As Integer = ROOT
         Dim LookUpTreeItem As New Integer

         ByteO = CompressedData.Dequeue()
         Do While (DecompressedData.Count < DecompressedSize - 1)
            If (ByteO And Bit) = &H0% Then
               If Leaf > LOOK_UP_TREE_SIZE Then Exit Do
               LookUpTreeItem = LookUpTree(Leaf)
            Else
               If Leaf + &H1% > LOOK_UP_TREE_SIZE Then Exit Do
               LookUpTreeItem = LookUpTree(Leaf + &H1%)
            End If

            Bit = (Bit << &H1%) And &HFFFF%
            If Bit > Byte.MaxValue Then
               Bit = &H1%
               ByteO = CompressedData.Dequeue()
            End If

            If (LookUpTreeItem And LOOK_UP_TREE_TAG) = LOOK_UP_TREE_TAG Then
               Leaf = (LookUpTreeItem And &HFF%) << &H1%
            Else
               DecompressedData.Add(ToByte(LookUpTreeItem And &HFF%))
               Leaf = ROOT
            End If
         Loop

         Return DecompressedData
      Catch ExceptionO As Exception
         Console.WriteLine($"Error: {ExceptionO.Message}")
      End Try

      Return Nothing
   End Function
End Module