using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 项目管理
    /// </summary>
    public class ProjectMgr : SingletonTemplate<ProjectMgr>
    {
        /// <summary>
        /// 当前项目变更委托
        /// </summary>
        /// <param name="strOldProject">旧项目</param>
        /// <param name="strNewProject">新项目</param>
        public delegate void CurrentProjectChangedHandler(string strOldProject, string strNewProject);

        private IniHelper m_ini;
        /// <summary>
        /// 所有的项目
        /// </summary>
        public List<string> AllProject
        {
            get
            {
                List<string> AllProject = new List<string>();
                string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project");
                DirectoryInfo dir = new DirectoryInfo(strPath);
                try
                {
                    DirectoryInfo dirD = dir;
                    if (dirD == null)
                    {
                        string strLog = string.Format("路径{0}的指向文件不是文件夹", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project", CurProject));
                        MessageBox.Show(strLog);
                        return AllProject;
                    }
                    FileSystemInfo[] files = dirD.GetFileSystemInfos();
                    FileSystemInfo[] array = files;
                    foreach (FileSystemInfo eachFile in array)
                    {
                        FileInfo file = eachFile as FileInfo;
                        if (file == null)
                        {
                            AllProject.Add(eachFile.Name);
                        }
                    }
                }
                catch (Exception)
                {
                }
                return AllProject;
            }
        }
        /// <summary>
        /// 当前项目
        /// </summary>
        public string CurProject
        {
            get
            {
                return m_ini.GetString("Project", "CurProject", "secote");
            }
            set
            {
                string strOldProject = CurProject;

                if (strOldProject != value)
                {
                    m_ini.WriteString("Project", "CurProject", value);
                    if (this.OnCurrentProjectChangedEvent != null)
                    {
                        this.OnCurrentProjectChangedEvent(strOldProject, value);
                    }
                }
            }
        }
        /// <summary>
        /// 当前项目路径
        /// </summary>
        public string CurProjectPath
        {
            get
            {
                string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project", CurProject);
                if (!Directory.Exists(strPath))
                {
                    Directory.CreateDirectory(strPath);
                }
                return strPath;
            }
        }
        /// <summary>
        /// 当前项目变更事件
        /// </summary>
        public event CurrentProjectChangedHandler OnCurrentProjectChangedEvent;
        /// <summary>
        /// 项目变更结束事件
        /// </summary>
        public event EventHandler OnProjectChangeFinishedEvent;
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectMgr()
        {
            string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project");
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            m_ini = new IniHelper(Path.Combine(strPath, "project.ini"));
        }
        /// <summary>
        /// 设置变更结束
        /// </summary>
        public void SetChangeFinished()
        {
            GC.Collect();
            this.OnProjectChangeFinishedEvent?.Invoke(null, new EventArgs());
        }
        /// <summary>
        /// 是否存在项目
        /// </summary>
        /// <param name="strProjectName">项目名称</param>
        /// <returns></returns>
        public bool IsExist(string strProjectName)
        {
            if (AllProject.IndexOf(strProjectName) >= 0)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 添加项目
        /// </summary>
        /// <param name="strProjectName">项目名称</param>
        /// <returns></returns>
        public bool AddProject(string strProjectName)
        {
            List<string> listProject = new List<string>(AllProject);
            if (listProject.IndexOf(strProjectName) >= 0)
            {
                LogHelper.Error("当前工程已存在");
                return false;
            }
            if (string.IsNullOrEmpty(strProjectName))
            {
                LogHelper.Error("当前工程名为空");
                return false;
            }
            listProject.Add(strProjectName);
            m_ini.WriteArray("Project", "AllProject", listProject.ToArray(), -1, ',');
            string strNewPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project", strProjectName);
            CopyDirectory(CurProjectPath, strNewPath);
            MessageBox.Show("新工程添加成功");
            return true;
        }
        /// <summary>
        /// 拷贝文件夹内所有内容到新的文件夹下面
        /// </summary>
        /// <param name="SourcePath"></param>
        /// <param name="DestinationPath"></param>
        /// <param name="overWriteExisting"></param>
        /// <returns></returns>
        private bool CopyDirectory(string SourcePath, string DestinationPath, bool overWriteExisting = true)
        {
            bool ret = false;
            try
            {
                SourcePath = (SourcePath.EndsWith("\\") ? SourcePath : (SourcePath + "\\"));
                DestinationPath = (DestinationPath.EndsWith("\\") ? DestinationPath : (DestinationPath + "\\"));
                if (Directory.Exists(SourcePath))
                {
                    if (!Directory.Exists(DestinationPath))
                    {
                        Directory.CreateDirectory(DestinationPath);
                    }
                    string[] files = Directory.GetFiles(SourcePath);
                    foreach (string fls in files)
                    {
                        FileInfo flinfo = new FileInfo(fls);
                        flinfo.CopyTo(DestinationPath + flinfo.Name, overWriteExisting);
                    }
                    string[] directories = Directory.GetDirectories(SourcePath);
                    foreach (string drs in directories)
                    {
                        DirectoryInfo drinfo = new DirectoryInfo(drs);
                        if (!CopyDirectory(drs, DestinationPath + drinfo.Name, overWriteExisting))
                        {
                            ret = false;
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 删除项目
        /// </summary>
        /// <param name="strProjectName"></param>
        /// <returns></returns>
        public bool RemoveProject(string strProjectName)
        {
            List<string> listProject = new List<string>(AllProject);
            if (listProject.Count <= 1)
            {
                LogHelper.Error("当前工程仅有1个，请先添加再删除");
                return false;
            }
            if (strProjectName == CurProject)
            {
                LogHelper.Error("删除工程为当前工程，请重新选择");
                return false;
            }
            if (listProject.IndexOf(strProjectName) >= 0)
            {
                listProject.Remove(strProjectName);
                m_ini.WriteArray("Project", "AllProject", listProject.ToArray(), -1, ',');
            }
            string strPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project", strProjectName);
            DirectoryInfo DInfo = new DirectoryInfo(strPath);
            DInfo.Delete(true);
            return true;
        }
    }
}
