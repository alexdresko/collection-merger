<Query Kind="Program">
  <NuGetReference>Exceptionless.DateTimeExtensions</NuGetReference>
</Query>

void Main()
{
	var destinationPeople = new List<Person>
	{
		new Person { ID = 1, Name = "Person 1 will be updated",
			Cats =
			[ 
				new() { ID = 1, Name = "Cat 1 be updated" },
				new() { ID = 2, Name = "Cat 2 will be removed" }
			]},  
        new Person { ID = 4, Name = "Person 4 will be removed" }
	};

	var sourcePeople = new List<PersonDto>
	{
		new PersonDto { ID = 1, Name = "Updated person 1 name",
			Cats =
			[ 
				new() { ID = 1, Name = "Updated cat 1 name" },
				new() { ID = 3, Name = "Added cat 3" },
			]},  
		new PersonDto { ID = 2, Name = "Person 2 will be added",
			Cats =
			[ 
				new() { ID = 4, Name = "Cat 4 will be added" }
			]},  
		// This person will be added
		new PersonDto { ID = 3, Name = "Person 3 will be added" }
	};

	var report = destinationPeople.MapFrom(
		source: sourcePeople,
		matchPredicate: (srcPerson, destPerson) => srcPerson.ID == destPerson.ID,
		mapProperties: (srcPerson, destPerson, m1) =>
		{
			destPerson.ID = srcPerson.ID;
			destPerson.Name = srcPerson.Name;

			destPerson.Cats.MapFrom(
				parent: m1,
				source: srcPerson.Cats,
				matchPredicate: (srcCat, destCat) => srcCat.ID == destCat.ID,
				mapProperties: (srcCat, destCat, m2) =>
				{
					destCat.ID = srcCat.ID;
					destCat.Name = srcCat.Name;
				});
		});

	// No Dump() calls (library proof-of-concept now lives in CollectionMerger).
}


public static class CollectionSyncExtensions
{
	/// <summary>
	/// Root-level MapFrom that creates a new mapper context.
	/// </summary>
	public static SyncReport MapFrom<TSource, TDestination>(
		this List<TDestination> destination,
		List<TSource> source,
		Func<TSource, TDestination, bool> matchPredicate,
		Action<TSource, TDestination, Mapper> mapProperties)
		where TDestination : new()
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(matchPredicate);
		ArgumentNullException.ThrowIfNull(mapProperties);

		var mapper = new Mapper();

		MapFromInternal(
			destination,
			source,
			matchPredicate,
			mapProperties,
			mapper,
			null,
			typeof(TDestination).Name);

		return mapper.GetReport();
	}

	/// <summary>
	/// Nested MapFrom that uses an existing mapper context from the parent.
	/// </summary>
	public static void MapFrom<TSource, TDestination>(
		this List<TDestination> destination,
		Mapper parent,
		List<TSource> source,
		Func<TSource, TDestination, bool> matchPredicate,
		Action<TSource, TDestination, Mapper> mapProperties)
		where TDestination : new()
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(matchPredicate);
		ArgumentNullException.ThrowIfNull(mapProperties);
		ArgumentNullException.ThrowIfNull(parent);

		MapFromInternal(
			destination,
			source,
			matchPredicate,
			mapProperties,
			parent,
			parent.CurrentPath,
			typeof(TDestination).Name);
	}

	private static void MapFromInternal<TSource, TDestination>(
		List<TDestination> destination,
		List<TSource> source,
		Func<TSource, TDestination, bool> matchPredicate,
		Action<TSource, TDestination, Mapper> mapProperties,
		Mapper mapper,
		string parentPath,
		string collectionName)
		where TDestination : new()
	{
		// Update existing destination items or add new ones.
		foreach (var sourceItem in source)
		{
			var destItem = destination.FirstOrDefault(d => matchPredicate(sourceItem, d));
			if (destItem != null)
			{
				// Capture state before mapping
				var beforeState = CaptureState(destItem);

				// Set current path for nested operations
				var itemPath = BuildPath(parentPath, collectionName, destItem);
				mapper.PushPath(itemPath);

				// Update the existing destination item.
				mapProperties(sourceItem, destItem, mapper);

				mapper.PopPath();

				// Capture state after mapping
				var afterState = CaptureState(destItem);

				// Detect changes
				var changes = DetectChanges(beforeState, afterState);
				if (changes.Any())
				{
					mapper.RecordUpdate(itemPath, destItem, changes);
				}
			} else
			{
				// Create a new destination item, map properties, and add it.
				var newItem = new TDestination();

				var itemPath = BuildPath(parentPath, collectionName, newItem);
				mapper.PushPath(itemPath);

				mapProperties(sourceItem, newItem, mapper);

				mapper.PopPath();

				destination.Add(newItem);
				mapper.RecordAdd(itemPath, newItem);
			}
		}

		// Remove destination items that no longer have a matching source item.
		var toRemove = destination
			.Where(dest => !source.Any(src => matchPredicate(src, dest)))
			.ToList();

		foreach (var item in toRemove)
		{
			destination.Remove(item);
			var itemPath = BuildPath(parentPath, collectionName, item);
			mapper.RecordRemove(itemPath, item);
		}
	}

	private static string BuildPath(string parentPath, string collectionName, object item)
	{
		var idProp = item.GetType().GetProperty("ID");
		var id = idProp?.GetValue(item)?.ToString() ?? "?";

		if (string.IsNullOrEmpty(parentPath))
			return $"{collectionName}[{id}]";

		return $"{parentPath}.{collectionName}[{id}]";
	}

	private static Dictionary<string, object> CaptureState<T>(T obj)
	{
		var state = new Dictionary<string, object>();
		var properties = typeof(T).GetProperties(
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.Instance);

		foreach (var prop in properties)
		{
			if (prop.CanRead && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType)
				|| prop.PropertyType == typeof(string))
			{
				var value = prop.GetValue(obj);
				state[prop.Name] = value;
			}
		}

		return state;
	}

	private static List<PropertyChange> DetectChanges(
		Dictionary<string, object> before,
		Dictionary<string, object> after)
	{
		var changes = new List<PropertyChange>();

		foreach (var key in before.Keys)
		{
			if (after.ContainsKey(key))
			{
				var oldValue = before[key];
				var newValue = after[key];

				bool changed = false;
				if (oldValue == null && newValue != null)
					changed = true;
				else if (oldValue != null && newValue == null)
					changed = true;
				else if (oldValue != null && newValue != null && !oldValue.Equals(newValue))
					changed = true;

				if (changed)
				{
					changes.Add(new PropertyChange
					{
						PropertyName = key,
						OldValue = oldValue,
						NewValue = newValue
					});
				}
			}
		}

		return changes;
	}
}

public class Mapper
{
	private readonly Stack<string> _pathStack = new();
	private readonly List<ChangeRecord> _changes = new();

	public string CurrentPath => _pathStack.Count > 0 ? _pathStack.Peek() : null;

	internal void PushPath(string path)
	{
		_pathStack.Push(path);
	}

	internal void PopPath()
	{
		if (_pathStack.Count > 0)
			_pathStack.Pop();
	}

	internal void RecordUpdate(string path, object item, List<PropertyChange> propertyChanges)
	{
		_changes.Add(new ChangeRecord
		{
			ChangeType = ChangeType.Updated,
			Path = path,
			Item = item,
			PropertyChanges = propertyChanges
		});
	}

	internal void RecordAdd(string path, object item)
	{
		_changes.Add(new ChangeRecord
		{
			ChangeType = ChangeType.Added,
			Path = path,
			Item = item
		});
	}

	internal void RecordRemove(string path, object item)
	{
		_changes.Add(new ChangeRecord
		{
			ChangeType = ChangeType.Removed,
			Path = path,
			Item = item
		});
	}

	internal SyncReport GetReport()
	{
		return new SyncReport
		{
			Changes = _changes
		};
	}
}

public class SyncReport
{
	public List<ChangeRecord> Changes { get; set; } = new();

	public int TotalChanges => Changes.Count;

	public int UpdatedCount => Changes.Count(c => c.ChangeType == ChangeType.Updated);
	public int AddedCount => Changes.Count(c => c.ChangeType == ChangeType.Added);
	public int RemovedCount => Changes.Count(c => c.ChangeType == ChangeType.Removed);
}

public class ChangeRecord
{
	public ChangeType ChangeType { get; set; }
	public string Path { get; set; }
	public object Item { get; set; }
	public List<PropertyChange> PropertyChanges { get; set; } = new();
}

public enum ChangeType
{
	Added,
	Updated,
	Removed
}

public class PropertyChange
{
	public string PropertyName { get; set; }
	public object OldValue { get; set; }
	public object NewValue { get; set; }
}

public class Person
{
	public int ID { get; set; }
	public string Name { get; set; }

	public List<Cat> Cats { get; set; } = new();
}

public class PersonDto
{
	public int ID { get; set; }
	public string Name { get; set; }

	public List<CatDto> Cats { get; set; } = new();
}

public class Cat
{
	public int ID { get; set; }
	public string Name { get; set; }
}

public class CatDto
{
	public int ID { get; set; }
	public string Name { get; set; }
}