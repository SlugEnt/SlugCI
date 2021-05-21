SlugCI

# How it Works

There are 3 deployment branches that we consider
 - main 
 - alpha
 - beta

In addition to those 3, there are 2 sub branches that we consdier:
 - fix / bug
 - Feature

## Fix / Bug branches 
When deployed will bump the Patch Version number by 1 if it has not already been bumped.  It will always increment the -alpha or -beta PreRelease tag.

## Feature branches
When deployed will bump the Minor version number by 1 if not already done and will always bump the -alpha or -beta Prerelease version number by 1.

## How version bumping works
The choices that affect the version number:
 - DeployTo parameter.  
 - CurrentBranch of the Git tree.

**Current Branch:  Fix/Bug**

**DeployTo:  Alpha / Beta**

IF there is not 






