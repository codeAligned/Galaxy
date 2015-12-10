import shutil 
import os
import xml.etree.cElementTree as etree
import time

appName = 'Galaxy.DealManager.exe'
configFileName = 'Galaxy.DealManager.exe.config'
# remotePath = r'Binary'
remotePath = r'v:\PROD\DealManager\Binary'
localPath = r'c:\Homeware\PROD\DealManager\Binary'

def CopyLatest():
    print('Copy latest version ')
    shutil.copytree(remotePath, localPath)
    return

def UpdateVersion():
    print('Remove local version ')
    shutil.rmtree(localPath)
    CopyLatest()
    return

def LoadXmlElement(configFile, elementName):
    tree = etree.parse(configFile)
    root = tree.getroot()

    for neighbor in root.iter('add'):
        key = neighbor.get('key')
        value = neighbor.get('value')
        if(key == elementName):
            return value
    return ''

print('Deploy ' + appName + ' in progress...')

if (os.path.exists(remotePath) == False):
    print('Enable to find the app to deploy ')
    time.sleep(5)
    quit()

if (os.path.exists(localPath) == False):
    CopyLatest()
else:
    srcVersionFile = os.path.join(remotePath,configFileName)
    dstVersionFile = os.path.join(localPath,configFileName)
    if(os.path.exists(srcVersionFile) == False or os.path.exists(dstVersionFile) == False):
        print('Enable to find the app config file')
        time.sleep(5)
        quit()
    else:
        srcVersion = LoadXmlElement(srcVersionFile, 'Version')
        print('Last version: ' + srcVersion)
        dstVersion = LoadXmlElement(dstVersionFile, 'Version')
        print('Your version: ' + dstVersion)
        if(srcVersion == '' or dstVersion == '' or srcVersion != dstVersion):
            UpdateVersion()
        else:
            print('current version up to date')

# Start program
print ('Start ' + appName ) 
os.chdir(localPath)
os.startfile(appName)
time.sleep(5)
quit()