using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;



namespace CLADAQ
{
    class Browser
    {
        public static void PopulateTreeView(TreeView treeView)
        {
            TreeNode rootNode;

            //DirectoryInfo info = new DirectoryInfo(@"../..");
            DirectoryInfo info = new DirectoryInfo(@"E:\data-cladaq\");
            if (info.Exists)
            {
                rootNode = new TreeNode(info.Name);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                treeView.Nodes.Add(rootNode);
            }
        }

        private static void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                    aNode.ImageIndex = 0;
                }
                else
                { aNode.ImageIndex = 1; }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }
    }
}
