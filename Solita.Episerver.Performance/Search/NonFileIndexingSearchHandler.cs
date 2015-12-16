using EPiServer.Search;

namespace Solita.Episerver.Performance.Search
{
    /// <summary>
    /// By default Episerver.Search indexes all file content for text, including xml, 
    /// js, css, json, etc. With big files this is CPU intense and slow, and often there 
    /// is no need for full-text search with files. 
    ///       
    /// This override removes link to the file content before sending the item to 
    /// processing, thus preventing the file content indexing. It'll keep other indexable 
    /// properties intact.
    /// </summary>
    public class NonFileIndexingSearchHandler : SearchHandler
    {
        public override void UpdateIndex(IndexRequestItem item)
        {
            // never index data content. it's too slow
            item.DataUri = null;

            base.UpdateIndex(item);
        }

        public override void UpdateIndex(IndexRequestItem item, string namedIndexingService)
        {
            // never index data content. it's too slow
            item.DataUri = null;

            base.UpdateIndex(item, namedIndexingService);
        }
    }
}