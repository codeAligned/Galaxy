import shutil 
import os

appName = 'HelloWorld.exe'
folderName = 'App'
dstFolderName = 'MyApp'
remotePath = r'SharedDrive'
localPath = r'LocalDrive'

def CopyLast():
    print('Copy last version ')
    shutil.copytree(srcPath, dstPath)
    return

def UpdateVersion():
    print('Remove local version ')
    shutil.rmtree(dstPath)
    CopyLast()
    return

def LoadVersionNb(versionPath):
    srcFile = open(versionPath)
    versionNb = srcFile.readline()
    srcFile.close()
    return versionNb


print('Deploy ' + appName + ' in progress...')
srcPath = os.path.join(remotePath,folderName)
dstPath = os.path.join(localPath,dstFolderName)
srcVersionFile = os.path.join(srcPath,'version.txt')
dstVersionFile = os.path.join(dstPath,'version.txt')

if (os.path.exists(dstPath) == False):
    CopyLast()
else:
	if(os.path.exists(srcVersionFile) == False or os.path.exists(dstVersionFile) == False):
		UpdateVersion()
	else:
		srcVersion = LoadVersionNb(srcVersionFile)
		print('Last version: ' + srcVersion)
		dstVersion = LoadVersionNb(dstVersionFile)
		print('Your version: ' + dstVersion)
		if(srcVersion != dstVersion):
			UpdateVersion()

# Start program
print ('Start ' + folderName ) 
os.chdir(dstPath)
os.startfile(appName)