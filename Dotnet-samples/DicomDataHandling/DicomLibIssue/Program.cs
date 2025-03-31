// See https://aka.ms/new-console-template for more information

using DicomLibIssue;

IDicomWriter writer = new FileStoreWriter();
var storeManager = new StoreManager(writer);

var warmupImage = @"C:\WorkingDirectory\DummyData\warmup\IM1";
storeManager.StoreInstance(warmupImage);

var fileName = args[0];
await storeManager.StoreInstance(fileName);
