﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Windows.Forms;
using System.Xml;

using ICSharpCode.XmlEditor;
using NUnit.Framework;
using XmlEditor.Tests.Utils;

namespace XmlEditor.Tests.Tree
{
	/// <summary>
	/// Various tests for the XmlTreeViewContainerControl. These
	/// tests do not really fit into any other test fixture.
	/// </summary>
	[TestFixture]
	public class XmlTreeViewContainerTestFixture
	{
		XmlDocument doc;
		XmlTreeViewControl treeView;
		DerivedXmlTreeViewContainerControl treeViewContainer;
		RichTextBox textBox;
		XmlCompletionDataProvider provider;
		PropertyGrid attributesGrid;
		SplitContainer splitContainer;	
		RichTextBox errorMessageTextBox;
		bool dirtyChanged;
		XmlElementTreeNode htmlTreeNode;
		XmlTextTreeNode textTreeNode;
		
		[SetUp]
		public void Init()
		{
			treeViewContainer = new DerivedXmlTreeViewContainerControl();
			treeViewContainer.DirtyChanged += TreeViewContainerDirtyChanged;
							
			XmlTextReader reader = ResourceManager.GetXhtmlStrictSchema();
			XmlSchemaCompletionData xhtmlSchema = new XmlSchemaCompletionData(reader);
			XmlSchemaCompletionDataCollection schemas = new XmlSchemaCompletionDataCollection();
			provider = new XmlCompletionDataProvider(schemas, xhtmlSchema, String.Empty);
			
			treeViewContainer.LoadXml("<html id='a'>text<body></body></html>", provider);
			doc = treeViewContainer.Document;
			treeView = treeViewContainer.TreeView;
			
			htmlTreeNode = (XmlElementTreeNode)treeView.Nodes[0];
			htmlTreeNode.Expanding();
			textTreeNode = (XmlTextTreeNode)htmlTreeNode.Nodes[0];

			splitContainer = (SplitContainer)treeViewContainer.Controls["splitContainer"];
			
			textBox = (RichTextBox)splitContainer.Panel2.Controls["textBox"];
			errorMessageTextBox = (RichTextBox)splitContainer.Panel2.Controls["errorMessageTextBox"];
			attributesGrid = (PropertyGrid)splitContainer.Panel2.Controls["attributesGrid"];
		}
		
		[TearDown]
		public void TearDown()
		{
			if (treeViewContainer != null) {
				treeViewContainer.DirtyChanged -= TreeViewContainerDirtyChanged;
				treeViewContainer.Dispose();
			}
		}
		
		[Test]
		public void ErrorMessageTextBoxVisibleAtStart()
		{
			Assert.IsFalse(treeViewContainer.IsErrorMessageTextBoxVisible);
		}
		
		[Test]
		public void TextBoxIsNotTabStopAtStart()
		{
			Assert.IsFalse(textBox.TabStop);
		}
		
		[Test]
		public void TextBoxIsEmptyAtStart()
		{
			Assert.AreEqual(String.Empty, textBox.Text);
		}
		
		[Test]
		public void AttributesGridIsTabStopAtStart()
		{
			Assert.IsTrue(attributesGrid.TabStop);
		}
		
		/// <summary>
		/// Check that the XmlTreeViewContainer brought the
		/// AttributesGrid to the front.
		/// </summary>
		[Test]
		public void AttributesGridOnTop()
		{
			Assert.AreEqual(0, splitContainer.Panel2.Controls.IndexOf(attributesGrid), 
				"AttributesGrid is not on top");
		}

		/// <summary>
		/// Checks that the text box shows the specified text and
		/// is visible in the control.
		/// </summary>
		[Test]
		public void ShowTextContent()
		{
			string text = "test";
			treeViewContainer.ShowTextContent(text);
			
			Assert.AreEqual(text, treeViewContainer.TextContent);
			Assert.AreEqual(text, textBox.Text);
			Assert.IsTrue(textBox.TabStop);
			Assert.AreEqual(0, splitContainer.Panel2.Controls.IndexOf(textBox));
		}
		
		[Test]
		public void TextBoxClearedAfterLoadXml()
		{
			treeViewContainer.ShowTextContent("test");
			treeViewContainer.LoadXml("<html/>", provider);
			
			Assert.AreEqual(String.Empty, textBox.Text);
			AttributesGridOnTop();
		}
		
		[Test]
		public void RootNodeExpanded()
		{
			Assert.IsTrue(treeView.Nodes[0].IsExpanded);
		}
		
		[Test]
		public void AttributesClearedAfterLoadXml()
		{
			// Make sure some attributes are showing.
			treeViewContainer.ShowAttributes(doc.DocumentElement.Attributes);
			
			Assert.IsTrue(doc.DocumentElement.HasAttributes, "Sanity check that the root element has some attributes");
			Assert.IsNotNull(attributesGrid.SelectedObject);
			
			// Loading new xml should clear the attributes grid.
			treeViewContainer.LoadXml("<html/>", provider);
			
			Assert.IsNull(attributesGrid.SelectedObject, 
				"Should be no SelectedObject in the attributes grid after loading new xml.");
		}
		
		[Test]
		public void ErrorMessageTextBoxNotTabStop()
		{
			Assert.IsFalse(errorMessageTextBox.TabStop);
		}
		
		[Test]
		public void ErrorMessageTextBoxNotOnTop()
		{
			Assert.AreNotEqual(0, splitContainer.Panel2.Controls.IndexOf(errorMessageTextBox));
		}
		
		[Test]
		public void ShowXmlNotWellFormed()
		{
			XmlException ex = new XmlException("Message");
			treeViewContainer.ShowXmlIsNotWellFormedMessage(ex);
			
			Assert.AreEqual(0, treeView.Nodes.Count, "TreeView should be cleared.");
			Assert.AreEqual(ex.Message, treeViewContainer.ErrorMessage);
			Assert.AreEqual(0, splitContainer.Panel2.Controls.IndexOf(errorMessageTextBox), "ErrorMessageTextBox should be on top");
			Assert.AreEqual(ex.Message, errorMessageTextBox.Text);
			Assert.IsTrue(errorMessageTextBox.TabStop);
			Assert.IsFalse(attributesGrid.TabStop);
			Assert.IsFalse(textBox.TabStop);
		}
		
		/// <summary>
		/// Checks that the text box is not a tab stop after showing
		/// an error.
		/// </summary>
		[Test]
		public void ShowTextContentBeforeShowingError()
		{
			treeViewContainer.ShowTextContent("Test");
			ShowXmlNotWellFormed();
		}
		
		[Test]
		public void DirtyChanged()
		{
			treeViewContainer.IsDirty = true;
			Assert.IsTrue(dirtyChanged);
		}
		
		[Test]
		public void TextChanged()
		{
			// Select the text node.
			treeView.SelectedNode = textTreeNode;
			
			string newText = "changed text";
			textBox.Text = newText;
			
			// Make sure the dirty flag is changed by changing
			// the text.
			treeViewContainer.IsDirty = false;
			dirtyChanged = false;
			
			treeViewContainer.CallTextBoxTextChanged();
			
			Assert.AreEqual(newText, textTreeNode.XmlText.Value);
			Assert.AreEqual(newText, textTreeNode.Text, "Tree node text should be updated with new XmlText's value");
			Assert.IsTrue(treeViewContainer.IsDirty);
			Assert.IsTrue(dirtyChanged);
		}
	
		/// <summary>
		/// Tests that when the XmlTreeView's UpdateTextNode method
		/// is called we do not get a null exception if the
		/// text node cannot be found in the tree.
		/// </summary>
		[Test]
		public void UpdateUnknownTextNodeText()
		{
			// Select the text node.
			treeView.SelectedNode = textTreeNode;
			
			XmlText textNode = doc.CreateTextNode(String.Empty);
			treeView.UpdateTextNode(textNode);
		}
		
		/// <summary>
		/// Updates the text node when no text node is selected in the
		/// tree.
		/// </summary>
		[Test]
		public void UpdateTextNodeText()
		{
			treeView.SelectedNode = null;
			
			textTreeNode.XmlText.Value = "New value";
			treeView.UpdateTextNode(textTreeNode.XmlText);
			Assert.AreEqual("New value", textTreeNode.Text);
		}
		
		/// <summary>
		/// Check that the DirtyChanged event is not fired
		/// </summary>
		[Test]
		public void TextChangedDirtyUnchanged()
		{
			// Select the text node.
			treeView.SelectedNode = textTreeNode;
			
			textBox.Text = "changed text";
			
			// Make sure the dirty flag is changed by changing
			// the text.
			treeViewContainer.IsDirty = true;
			dirtyChanged = false;
			
			treeViewContainer.CallTextBoxTextChanged();
			
			Assert.AreEqual("changed text", textTreeNode.XmlText.Value);
			Assert.IsTrue(treeViewContainer.IsDirty);
			Assert.IsFalse(dirtyChanged);
		}
		
		[Test]
		public void AttributeValueChanged()
		{
			// Select the html node.
			treeView.SelectedNode = htmlTreeNode;
			treeViewContainer.ShowAttributes(doc.DocumentElement.Attributes);
			
			Assert.IsNotNull(attributesGrid.SelectedGridItem);
			
			treeViewContainer.IsDirty = false;
			dirtyChanged = false;
			treeViewContainer.CallAttributesGridPropertyValueChanged();
			
			Assert.IsTrue(treeViewContainer.IsDirty);
			Assert.IsTrue(dirtyChanged);
		}
		
		[Test]
		public void AttributeValueChangedDirtyUnchanged()
		{
			// Select the html node.
			treeView.SelectedNode = htmlTreeNode;
			treeViewContainer.ShowAttributes(doc.DocumentElement.Attributes);
			
			Assert.IsNotNull(attributesGrid.SelectedGridItem);
			
			treeViewContainer.IsDirty = true;
			dirtyChanged = false;
			treeViewContainer.CallAttributesGridPropertyValueChanged();
			
			Assert.IsTrue(treeViewContainer.IsDirty);
			Assert.IsFalse(dirtyChanged);
		}
		
		[Test]
		public void TextNodeSelected()
		{
			treeView.SelectedNode = textTreeNode;
			treeViewContainer.ShowTextContent(String.Empty);
			treeViewContainer.CallXmlElementTreeViewAfterSelect();
			
			Assert.AreEqual("text", textBox.Text);
		}
		
		[Test]
		public void HtmlElementNodeSelected()
		{
			treeView.SelectedNode = htmlTreeNode;
			treeViewContainer.CallXmlElementTreeViewAfterSelect();
			
			Assert.IsNotNull(attributesGrid.SelectedGridItem);
			Assert.AreEqual("id", attributesGrid.SelectedGridItem.Label);
		}
		
		[Test]
		public void XmlElementTreeNodeImageKey()
		{
			Assert.IsTrue(treeView.ImageList.Images.ContainsKey(XmlTextTreeNode.XmlTextTreeNodeImageKey));
		}
		
		[Test]
		public void XmlTextTreeNodeImageKey()
		{
			Assert.IsTrue(treeView.ImageList.Images.ContainsKey(XmlElementTreeNode.XmlElementTreeNodeImageKey));
		}
		
		[Test]
		public void XmlCommentTreeNodeImageKey()
		{
			Assert.IsTrue(treeView.ImageList.Images.ContainsKey(XmlCommentTreeNode.XmlCommentTreeNodeImageKey));
		}
		
		/// <summary>
		/// Checks that setting the TextContent property updates
		/// the text in the text box.
		/// </summary>
		[Test]
		public void TextBoxChanged()
		{
			treeViewContainer.TextContent = "Test";
			Assert.AreEqual("Test", textBox.Text);
		}
		
		[Test]
		public void DocumentMatches()
		{
			XmlDocument doc = new XmlDocument();
			treeView.Document = doc;
			Assert.AreSame(doc, treeView.Document);
		}
		
		void TreeViewContainerDirtyChanged(object source, EventArgs e)
		{
			dirtyChanged = true;
		}
	}
}
