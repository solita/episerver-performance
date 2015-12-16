# Solita EPiServer Performance 

Performance hacks for Episerver 9

## Configuration example

```
[ModuleDependency(typeof(ServiceContainerInitialization))]
[InitializableModule]
public class MvcTemplatesInitializer : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {       
        // Configure StructureMap
        StructureMapConfig.ConfigureContainer(context.Container);
    }
}
```

```
    public class StructureMapConfig
    {
        public static void ConfigureContainer(IContainer container)
        {
            container.Configure(x =>
            {
                // Use NonFileIndexingSearchHandler to disable file content indexing
                x.For<SearchHandler>().Use<NonFileIndexingSearchHandler>();
                
                // Use CachingUrlResolver for better performance
                x.For<UrlResolver>().Use<CachingUrlResolver>();
            });
        }
    }
```

## CachingUrlResolver
Caches UrlResolver.GetVirtualPath(ContentReference, string, VirtualPathArguments) result in ContextMode.Default (end-user view). Cache has dependency for DataFactoryCache.Version which will keep it in-sync with changes in Episerver.

## NonFileIndexingSearchHandler
By default Episerver.Search indexes all file content for text, including xml, js, css, json, etc. With big files this is CPU intense and slow, and often there is no need for full-text search with files. This override removes link to the file content before sending the item to processing, thus preventing the file content indexing. It'll keep other indexable properties intact.