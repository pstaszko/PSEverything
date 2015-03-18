﻿using System;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSEverything
{
    [Cmdlet(VerbsCommon.Search, "Everything", SupportsPaging = true, DefaultParameterSetName = "default")]
    [OutputType(typeof(string))]
    [Alias("se")]
    public class SearchEverythingCommand : PSCmdlet
    {
        [Parameter(ParameterSetName = "default")]        
        public string QueryFilter { get; set; }

        [Parameter(ParameterSetName = "default", Position = 1)]        
        public string[] Include { get; set; }
        
        [Parameter(ParameterSetName = "default")]        
        public string[] Exclude { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string[] Extension { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string[] PathInclude { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string[] PathExclude { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string[] FolderInclude { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string[] FolderExclude { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public int ParentCount { get; set; }

        [Parameter(ParameterSetName = "default")]        
        public string ChildFileName { get; set; }
        
        [ValidateCount(1, 2)]
        [Parameter(ParameterSetName = "default")]        
        public int[] NameLength { get; set; }
        
        [ValidateCount(1,2)]
        [Parameter(ParameterSetName = "default")]        
        public long[] Size { get; set; }
        
        [Parameter(ParameterSetName = "regex")]
        public string RegularExpression { get; set; }
        
        [Parameter]
        public SwitchParameter CaseSensitive { get; set; }
        
        [Parameter(ParameterSetName = "default")]        
        public SwitchParameter MatchWholeWord { get; set; }
        
        private string GetSearchString()
        {
            if (ParameterSetName == "regex") { return RegularExpression; }

            var sb = new StringBuilder();
            AddPathFilter(sb);
            AddFileFilter(sb);
            AddFolderFilter(sb);
            AddPatternFilter(sb);                        
            AddParentCountFilter(sb);
            AddExtensionFilter(sb);
            AddChildFilter(sb);
            AddSizeFilter(sb);
            AddNameLengthFilter(sb);
            return sb.ToString();
        }

        private void AddPatternFilter(StringBuilder searchBuilder)
        {
            if (!String.IsNullOrEmpty(QueryFilter))
            {
                searchBuilder.Append(QueryFilter);
            }
        }

        private static void AddListFilter(StringBuilder searchBuilder, string filterName, string[] include, string[] exclude = null, char separator = ' ')
        {
            if (include == null && exclude == null) return;
            searchBuilder.Append(' ');
            if (include != null)
            {                
                foreach (var item in include)
                {                    
                    searchBuilder.Append(filterName);
                    searchBuilder.Append(item);
                    searchBuilder.Append(separator);
                }                
                
            }
            if (exclude != null)
            {
                foreach (var item in exclude)
                {
                    searchBuilder.Append(filterName);
                    searchBuilder.Append('!');
                    searchBuilder.Append(item);
                    searchBuilder.Append(separator);
                }
            }
            searchBuilder.Length--;
        }
        
        private void AddPathFilter(StringBuilder searchBuilder)
        {
            AddListFilter(searchBuilder, "path:", PathInclude, PathExclude);                    
        }

        void AddFileFilter(StringBuilder searchBuilder)
        {
            AddListFilter(searchBuilder, "file:", Include,Exclude);
        }

        void AddFolderFilter(StringBuilder searchBuilder)
        {
            AddListFilter(searchBuilder, "folder:", FolderInclude, FolderExclude);
        }

        void AddExtensionFilter(StringBuilder searchBuilder)
        {
            if (Extension == null) return;
            searchBuilder.Append(" ext:");
            
            foreach (var item in Extension)
            {                
                searchBuilder.Append(item);
                searchBuilder.Append(";");
            }
            searchBuilder.Length--;
        }

        void AddParentCountFilter(StringBuilder searchBuilder)
        {
            if (MyInvocation.BoundParameters.ContainsKey("ParentCount"))
            {
                searchBuilder.Append(" parents:");
                searchBuilder.Append(ParentCount);
            }
        }

        void AddChildFilter(StringBuilder searchBuilder)
        {
            if (!String.IsNullOrEmpty(ChildFileName))
            {
                searchBuilder.Append(" child:");
                searchBuilder.Append(ChildFileName);
            }
        }
      
        void AddSizeFilter(StringBuilder searchBuilder)
        {
            if (Size != null)
            {
                if (Size.Length == 1)
                {
                    searchBuilder.Append(" size:");
                    searchBuilder.Append(Size[0]);
                }
                else
                {
                    searchBuilder.Append(" size:");
                    searchBuilder.Append(Size[0]);
                    searchBuilder.Append("..");
                    searchBuilder.Append(Size[1]);
                }
            }
        }

        void AddNameLengthFilter(StringBuilder searchBuilder)
        {
            if (NameLength != null)
            {
                if (NameLength.Length == 1)
                {
                    searchBuilder.Append(" len:");
                    searchBuilder.Append(NameLength[0]);
                }
                else
                {
                    searchBuilder.Append(" len:");
                    searchBuilder.Append(NameLength[0]);
                    searchBuilder.Append("..");
                    searchBuilder.Append(NameLength[1]);
                }
            }
        }
        protected override void ProcessRecord()
        {
            
            Everything.SetMatchCase(CaseSensitive);
            Everything.SetMatchWholeWord(MatchWholeWord);
            Everything.SetRegEx(!String.IsNullOrEmpty(RegularExpression));            
                        
            ulong skip = PagingParameters.Skip;            
            if (skip > Int32.MaxValue)
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Cannot skip that many results"),"SkipToLarge", ErrorCategory.InvalidArgument, skip));
            }            

            ulong first = PagingParameters.First;

            if (first == ulong.MaxValue)
            {
                first = Int32.MaxValue;
            }
            if (first > Int32.MaxValue)
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Cannot take that many results"), "FirstToLarge", ErrorCategory.InvalidArgument, first));
            }                        

            var searchPattern = GetSearchString();
            WriteDebug("Search-Everything search pattern:" + searchPattern);
            Everything.SetSearch(searchPattern);

            Everything.Query(true);
            int resCount = Everything.GetTotalNumberOfResults();
            if (PagingParameters.IncludeTotalCount)
            {
                var total = PagingParameters.NewTotalCount((ulong) resCount , 1.0);
                WriteObject(total);
            }
            var res = Everything.GetAllResults(resCount);
            Array.Sort(res);
            if (skip == 0 && first == Int32.MaxValue)
            {
                WriteObject(res, enumerateCollection:true);
            }
            else 
            { 
                foreach (var path in res.Skip((int)skip).Take((int)first))            
                {
                    WriteObject(path);
                }
            }

        }
    }
}