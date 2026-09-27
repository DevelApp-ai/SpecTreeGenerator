using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSpecOptimizer.Common.Models;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Models
{
    /// <summary>
    /// Type of content section in a specification
    /// </summary>
    public enum ContentSectionType
    {
        Header,
        Description,
        Requirements,
        Examples,
        Notes,
        Custom
    }

    /// <summary>
    /// Represents a section of content in a specification
    /// </summary>
    public class ContentSection
    {
        /// <summary>
        /// Unique identifier for this section
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of content section
        /// </summary>
        public ContentSectionType Type { get; set; }
        
        /// <summary>
        /// Title of the section
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Content of the section
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this section is inherited from a parent specification
        /// </summary>
        public bool IsInherited { get; set; }
        
        /// <summary>
        /// Parent section ID (if inherited)
        /// </summary>
        public string ParentSectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this content section
        /// </summary>
        public ContentSection Clone()
        {
            return new ContentSection
            {
                Id = this.Id,
                Type = this.Type,
                Title = this.Title,
                Content = this.Content,
                IsInherited = this.IsInherited,
                ParentSectionId = this.ParentSectionId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Returns a string representation of the content section
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Title} ({(IsInherited ? "Inherited" : "Local")})";
        }
    }

    /// <summary>
    /// Represents a gene in the specification genome
    /// </summary>
    public class SpecificationGene
    {
        /// <summary>
        /// Unique identifier for this gene
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the specification
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Component ID this specification is for
        /// </summary>
        public string ComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Parent gene ID (if any)
        /// </summary>
        public string ParentGeneId { get; set; } = string.Empty;
        
        /// <summary>
        /// Content sections in this specification
        /// </summary>
        public List<ContentSection> ContentSections { get; set; } = new List<ContentSection>();
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this gene
        /// </summary>
        public SpecificationGene Clone()
        {
            var clone = new SpecificationGene
            {
                Id = this.Id,
                Name = this.Name,
                ComponentId = this.ComponentId,
                ParentGeneId = this.ParentGeneId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
            
            foreach (var section in this.ContentSections)
            {
                clone.ContentSections.Add(section.Clone());
            }
            
            return clone;
        }
        
        /// <summary>
        /// Converts this gene to a specification file
        /// </summary>
        public SpecificationFile ToSpecificationFile()
        {
            // Combine all content sections into a single markdown document
            var content = new System.Text.StringBuilder();
            
            foreach (var section in ContentSections.OrderBy(s => s.Type))
            {
                if (section.Type == ContentSectionType.Header)
                {
                    content.AppendLine($"# {section.Title}");
                }
                else
                {
                    content.AppendLine($"## {section.Title}");
                }
                
                content.AppendLine();
                content.AppendLine(section.Content);
                content.AppendLine();
            }
            
            return new SpecificationFile
            {
                Id = this.Id,
                Name = this.Name,
                ComponentId = this.ComponentId,
                ParentSpecificationId = this.ParentGeneId,
                Content = content.ToString(),
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Creates a gene from a specification file
        /// </summary>
        public static SpecificationGene FromSpecificationFile(SpecificationFile specFile)
        {
            var gene = new SpecificationGene
            {
                Id = specFile.Id,
                Name = specFile.Name,
                ComponentId = specFile.ComponentId,
                ParentGeneId = specFile.ParentSpecificationId,
                Metadata = new Dictionary<string, object>(specFile.Metadata)
            };
            
            // Parse content into sections
            // This is a simplified implementation - a real implementation would use a proper markdown parser
            var lines = specFile.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            ContentSection currentSection = null;
            var contentBuilder = new System.Text.StringBuilder();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("# "))
                {
                    // If we have a current section, add it to the gene
                    if (currentSection != null)
                    {
                        currentSection.Content = contentBuilder.ToString().Trim();
                        gene.ContentSections.Add(currentSection);
                        contentBuilder.Clear();
                    }
                    
                    // Create a new header section
                    currentSection = new ContentSection
                    {
                        Type = ContentSectionType.Header,
                        Title = line.Substring(2).Trim()
                    };
                }
                else if (line.StartsWith("## "))
                {
                    // If we have a current section, add it to the gene
                    if (currentSection != null)
                    {
                        currentSection.Content = contentBuilder.ToString().Trim();
                        gene.ContentSections.Add(currentSection);
                        contentBuilder.Clear();
                    }
                    
                    // Create a new section based on the title
                    var title = line.Substring(3).Trim();
                    var type = DetermineContentType(title);
                    
                    currentSection = new ContentSection
                    {
                        Type = type,
                        Title = title
                    };
                }
                else
                {
                    // Add to current section content
                    contentBuilder.AppendLine(line);
                }
            }
            
            // Add the last section
            if (currentSection != null)
            {
                currentSection.Content = contentBuilder.ToString().Trim();
                gene.ContentSections.Add(currentSection);
            }
            
            return gene;
        }
        
        private static ContentSectionType DetermineContentType(string title)
        {
            if (title.Contains("Requirement", StringComparison.OrdinalIgnoreCase))
                return ContentSectionType.Requirements;
            
            if (title.Contains("Example", StringComparison.OrdinalIgnoreCase))
                return ContentSectionType.Examples;
            
            if (title.Contains("Note", StringComparison.OrdinalIgnoreCase))
                return ContentSectionType.Notes;
            
            if (title.Contains("Description", StringComparison.OrdinalIgnoreCase))
                return ContentSectionType.Description;
            
            return ContentSectionType.Custom;
        }
        
        /// <summary>
        /// Returns a string representation of the gene
        /// </summary>
        public override string ToString()
        {
            return $"Gene: {Name} (Component: {ComponentId}, Sections: {ContentSections.Count})";
        }
    }
}
