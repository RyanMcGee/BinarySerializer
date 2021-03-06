BinarySerializer
================

A .NET declarative serialization framework for controlling formatting of data at the byte level.  BinarySerializer is designed to make working with binary formats and protocols fast and simple.  The Portable Class Library (PCL) is available on [nuget](https://www.nuget.org/packages/BinarySerializer).

### Field Ordering ###

There is no completely reliable way to get member ordering from the CLR so as of 3.0 <code>FieldOrder</code> attributes are required on all classes with more than one field or property.  By convention, base classes are serialized first followed by any derived classes.  For example, the following class will serialize in the order A, B, C.

    public class MyBaseClass
    {
        public int A { get; set; }
    }

    public class MyDerivedClass : MyBaseClass
    {
        [FieldOrder(0)]
        public int B { get; set; }

        [FieldOrder(1)]
        public int C;
    }

    ...

    var stream = new MemoryStream();
    var serializer = new BinarySerializer();
    
    var myDerivedClass = new MyDerivedClass();

    serializer.Serialize(stream, myDerivedClass);

    ...

Note that we're using properties and fields interchangeably as they are treated equivalently by the serializer.

### Binding ###

The most powerful feature of BinarySerializer is the ability to bind attributes to other fields in the object graph.  Using the available attributes this approach can allow for interop with complex formats and protocols.  One of the simplest examples of this is field length binding.

    public class Person
    {
        [FieldOrder(0)]
        public byte NameLength { get; set; }

        [FieldOrder(1)]
        [FieldLength("NameLength")]
        public string Name { get; set; }
    }
    
    ...
    
    var person = new Person { Name = "Alice" };

![](/BinarySerializer.Docs/SimpleBinding_2.png)

Note that it is not necessary that NameLength contains the length of the Name field as that value will be computed during serialization and updated in the serialized graph.  During deserialization the NameLength value will be used to correctly deserialize the Name field.

### Default Behavior ###

Although most behavior can be overridden, in many cases the serializer will attempt to guess the intended behavior based on class design.  For example, in the following class a null-terminated string will be used during serialization as deserialization would otherwise be impossible as defined.

    public class Person2
    {
        [FieldOrder(0)]
        public string Name { get; set; }

        [FieldOrder(1)]
        public int Age { get; set; }
    }


Attributes
----------

There are a number of attributes that can be used to control how your fields are serialized.

### IgnoreAttribute ###

Any field or property with an Ignore attribute will not be included in serialization or deserialization.  These fields can still be used in bindings, however properties will be treated as flat fields.  If you need to do some calculation on a binding source your best option is to define a ValueConverter (see below).

### SerializeAsAttribute ###

In general you shouldn't need this as most things tend to work out without it.  However, you can always override the default behavior by specifying SerializeAs.  This attribute can also be used to specify encodings and endianness if needed.


    [SerializeAs(Encoding = "windows-1256")]
    public string Name { get; set;  }
    
    [SerializeAs(Endianness = Endianness.Big)]
    public uint SectorCountBig { get; set; }

### FieldOrderAttribute ###

This attribute is required on any field/property in a class with more than one field or property.  Only relative value matters.  Base values are serialized before dervied values.

    public class MyBaseClass
    {
        public int A { get; set; }
    }

    public class MyDerivedClass : MyBaseClass
    {
        [FieldOrder(0)]
        public int B { get; set; }

        [FieldOrder(1)]
        public int C;
    }

### FieldLengthAttribute ###

FieldLength can be used to either specify bound or constant field length.  Field lengths can apply to anything that is sizeable; strings, arrays, lists, streams, and even complex objects.

    public class MyConstFieldClass
    {
        [FieldLength(32)]
        public string Name { get; set; }
    }

Alternatively, the length of the Name field could be bound to a NameLength field:

    public class MyBoundFieldClass
    {
        [FieldOrder(0)]
        public int NameLength { get; set; }

        [FieldOrder(1)]
        [FieldLength("NameLength")]
        public string Name { get; set; }
    }

In some cases you may want to limit a collection of items by the total serialized length.  Note that we are *not* restricting the number of items in the collection here, but the serialized length in bytes.

    public class MyBoundCollectionClass
    {
        [FieldOrder(0)]
        public int NameLength { get; set; }

        [FieldOrder(1)]
        [FieldLength("NameLength")]
        public List<string> Names { get; set; }
    }

Some formats and protocols will define a set of fields of specified size, with "optional" trailing fields.  In the following example, EntryLength will either be 32 or 36, depending on whether or not Age is specified.  

    public class Person
    {
        [FieldOrder(0)]
        [FieldLength(32)]
        public string Name { get; set; }

        [FieldOrder(1)]
        public int? Age { get; set; }
    }

    public class PersonEntry
    {
        [FieldOrder(0)]
        public int EntryLength { get; set; }

        [FieldOrder(1)]
        [FieldLength("EntryLength")]
        public Person Person { get; set; }
     }

If age is null during serialization, the framework will update EntryLength to be 32, or 36 if Age is present.  If EntryLength is 32 during deserialization, the framework will return a null value for Age.  If EntryLength is 36, the framework will deserialize the Age value.

### FieldCountAttribute ###

The FieldCount attribute is used to define how many items are contained in a collection.  In practice, this is either an array or a list with a single generic argument.

    public class Directory
    {
        [FieldOrder(0)]
        public int EntryCount { get; set; }

        [FieldOrder(1)]
        [FieldCount("EntryCount")]
        public List<Entry> Entries { get; set; }
    }

Note the special case of a byte array, for which length and count attributes are interchangeable.

### ItemLengthAttribute ###

This attribute can be used to control the length of items in a collection.

    public class Manifest
    {
        [FieldOrder(0)]
        public int EntryLength { get; set; }

        [FieldOrder(1)]
        [ItemLength("EntryLength")]
        public List<string> Entries { get; set; }
    }


### FieldOffsetAttribute ###

The FieldOffset attribute should be used sparingly but can be used if an absolute offset is required.  In most cases implicit offset (e.g. just define the structure) is preferable.  After moving to the offset the serializer will reset to the origin so subsequent fields must manage their own offsets.  This attribute is not supported when serializing to non-seekable streams.

### SubtypeAttribute ###

The Subtype attribute allows dynamic switching of subtypes based on a binding.

    public class Packet
    {
        [FieldOrder(0)]
        public FrameType FrameType { get; set; }

        [FieldOrder(1)]
        [Subtype("FrameType", FrameType.Message, typeof(MessageFrame)]
        [Subtype("FrameType", FrameType.Control, typeof(ControlFrame)]
        [Subtype("FrameType", FrameType.Trigger, typeof(TriggerFrame)]
        public Frame Frame { get; set; }
    }

It is not necessary that FrameType be correct during serialization; it will be updated with the appropriate value based on the instantiated type.  During deserialization the FrameType field will be used to construct the correct type.

### SerializeWhenAttribute ###

The SerializeWhen attribute can be used to conditionally serialize or deserialize a field based on bound predicate.  Multiple SerializeWhen attributes will be or'd together.

    [SerializeWhen("ControllerHardwareVersion", HardwareVersion.XBeeSeries1)]
    [SerializeWhen("ControllerHardwareVersion", HardwareVersion.XBeeProSeries1)]
    public ReceivedSignalStrengthIndicator ReceivedSignalStrengthIndicator { get; set; }

### SerializeUntilAttribute ###

The SerializedUntil attribute can be used to terminate a collection once a specified value is encountered, essentially allowing for the creation of "null-terminated" lists or the like.  This attribute is not currently supported when deserializing from non-seekable streams.

    [SerializeUntil((byte)0)]
    public List<DirectoryRecord> Records { get; set; }

### ItemSerializeUntilAttribute ###

The ItemSerializeUntil attribute can be used to terminate a collection when an item with a specified value is encountered.

    public class Toy
    {
        public string Name { get; set; }
        public bool IsLast { get; set; }
    }
     
    public class ToyChest
    {
        [ItemSerializeUntil("IsLast", true)]
        public List<Toy> Toys { get; set; }
    }

### SerializeAsEnumAttribute ###

The SerializeAsEnum attribute allows you specify an alternate value for an enum to be used during the operation.

    public enum Waypoints
    {
        [SerializeAsEnum("Alpha")]
        A,
        [SerializeAsEnum("Bravo")]
        B,
        [SerializeAsEnum("Charlie")]
        C
    }

-----------

### Performance ###

Serialization and deserialization operations are broken into four phases.

* Reflection
* Value assignment
* Binding
* Serialization/Deserialization

The first and most expensive stage is cached in memory for every type that the serializer encounters.  As such, it is best practice to create the serializer once and keep it around for subsequent operations.  If you are creating a new serializer each time, you'll be paying the reflection cost every time.

### Enums ###
Enums can be used to create expressive definitions.  Depending on what attributes are specified enums will be interpreted by the serializer as either the underlying value, the literal value of the enum, or a value specified with the SerializeAsEnum attribute.  In the following example, the field will be serialized using the enum underlying byte.

    public enum Shape : byte
    {
        Circle = 0x0,
        Square = 0x1
    }

    public class MyEnumClass
    {
        public Shape Shape { get; set; }
    }

Serializing this class would result in a single byte.  Alternatively, you may want the name of the enum to be serialized:

    public class MyEnumClass
    {
        [SerializeAs(SerializedType.NullTerminatedString)]
        public Shape Shape { get; set; }
    }

You could also specify this to be a fixed-sized string, etc.

### Streams ###

In some cases you may be serializing or deserializing large amounts of data, which is logically broken into blocks or volumes.  In these cases it may be adventageous to defer handling of those sections, rather than dealing with large in-memory buffers.

    [FieldOrder(22)]
    [SerializeWhen("RecordType", RecordType.File)]
    [FieldOffset("FirstSectorData", ConverterType = typeof(SectorByteConverter))]
    [FieldLength("DataLength")]
    public Stream Data { get; set; }

In this example, the Data property will be copied from the source stream during serialization.  On deserialization, the resulting object graph will contain a Streamlet object which references a section of the source stream and allows for deferred read access.  Note that this feature is only supported when the underlying source stream supports seeking.  When dealing with non-seekable streams (e.g. NetworkStream), it is better to deserialize the stream in frames or packets where possible rather than try to deserialize the entire stream (which in some cases may be open-ended) at once.


### Nulls ###

Null values are allowed; however, bear in mind that this can lead to unstable definitions.  While serialization may work, deserialization may fail if the framework is unable to deduce which fields are meant to be null and which are not.

### Nullable types ###

Nullable types are supported and are serialized, if present, as the underlying type.

### Advanced Binding ###

Binding is not limited to fields in the same object but can be used to reference arbitrary fields accessible throughout the graph.  Ancestors in the graph can be located by either type or level and can be used as references for binding.

    public class Container
    {
        [FieldOrder(0)]
        public int NameLength { get; set; }
        
        [FieldOrder(1)]
        public Person Person { get; set; }
    }
    
    public class Person
    {
        [FieldOrder(0)]
        [FieldLength("NameLength", Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(Container))]
        public string Name1 { get; set; }
        
        // is equivalent to
        
        [FieldOrder(1)]
        [FieldLength("NameLength", Mode = RelativeSourceMode.FindAncestor, AncestorLevel = 2)]
        public string Name2 { get; set; }
    }
    
#### Value Converter ####

Sometimes binding directly to a source is insuffient and in those cases your best option is to define a value converter, which can be specified as part of the binding.

    class SectorByteConverter : IValueConverter
    {
        public object Convert(object value, object converterParameter, BinarySerializationContext context)
        {
            var sector = (uint) value;
            var iso = context.FindAncestor<Iso9660>();
            var sectorSize = iso.PrimaryVolumeDescriptor.SectorSize;
            return sector*sectorSize;
        }

        public object ConvertBack(object value, object converterParameter, BinarySerializationContext context)
        {
            // etc
        }
    }
    
    
    [FieldOffset("FirstSector", ConverterType = typeof(SectorByteConverter))]
    [SerializeUntil((byte)0)]
    public List<DirectoryRecord> Records { get; set; }

### Custom Serialization ###

If all else fails, you can define a custom serialization object.

     /// <summary>
    /// A custom serializer for variable byte representation of an integer.
    /// </summary>
    public class Varuint : IBinarySerializable
    {
        public uint Value { get; set; }

        public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
        {
            var reader = new BinaryReader(stream);

            bool more = true;
            int shift = 0;

            Value = 0;

            while (more)
            {
                int b = reader.ReadByte();

                if (b == -1)
                    throw new InvalidOperationException("Reached end of stream before end of varuint.");

                var lower7Bits = (byte)b;
                more = (lower7Bits & 128) != 0;
                Value |= (uint)((lower7Bits & 0x7f) << shift);
                shift += 7;
            }
        }

        
        public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
        {
            var writer = new BinaryWriter(stream);

            var value = Value;
            do
            {
                var lower7Bits = (byte) (value & 0x7f);
                value >>= 7;
                if (value > 0)
                    lower7Bits |= 128;
                writer.Write(lower7Bits);
            } while (value > 0);
        }
    }

### Encoding ###

Text encoding can be specified by the <code>SerializeAsAttribute</code> and is inherited by all children unless overridden.

    [SerializeAs(Encoding = "windows-1256")]
    public string Name { get; set;  }
    
### Endianness ###

Maybe a quiant topic these days but incredibly painful if you're suddenly faced with it.  BinarySerializer handles endianness in two ways: globally or field-local.  If you're working in a system that deals entirely in big endian, you can simply do:

    serializer.Endianness = Endianness.Big;
    
In other cases you may actually have a mix of big and little endian and again you can use the <code>SerializeAsAttribute</code> to specify endianness.  As with encoding, endianness is inherited through the graph heirarchy unless overridden by a child field.

    [SerializeAs(Endianness = Endianness.Big)]
    public uint SectorCountBig { get; set; }
    
### Exceptions ###

If an exception does occur either during the initial reflection phase or subsequent serialization, every layer of the object graph will throw its own exception, keeping the prior exception as the inner exception.  Always check the inner exception for more details.

### Thread Safety ###

All public members of BinarySerializer are thread-safe and may be used concurrently from multiple threads.

## Examples ##

See [XBee](https://github.com/jefffhaynes/XBee) for a complete example of BinarySerializer use.
