# Cognition Design
This is the official project repository for Cognition Design <br> *HfG Offenbach am Main / Goethe University Cognition Lab*

## Basic Git Commands:

<p> Initialize local Git repository <br> <code> $ git init </code> </p>

<p> Add file(s) to index <br> <code> $ git add <file> </code> </p>

<p> Check status of working tree <br> <code> $ git status </code> </p>

<p> Commit changes in index <br> <code> $ git commit </code> </p>

<p> Commit specific file in a directory <br> <code> $ git commit -m 'my notes' path/to/my/file.ext </code> </p>

<p> Push to remote repository <br> <code> $ git push </code> </p>

<p> Pull latest from remote repository <br> <code> $ git pull </code> </p>

<p> Clone repository into a new directory <br> <code> $ git clone </code> </p>

<p> Git show pack size: <br> <code> $ git count-objects -Hv </code> </p>

<p> Git check ignored files: <br> <code> $ git check-ignore </code> </p>

<p> Bash command to find files larger than x <br> <code> $ find . -size +100M | xargs du -sh </code> </p>


<br> 

![Git Essential Workflow Diagram](https://it.mathworks.com/help/matlab/matlab_prog/srcctrl_git_diagram.png)

<br> 

<br> 

## Git LFS (Large File Storage)

<p> Git Large File Storage (LFS) replaces large files such as audio samples, videos, datasets, and graphics with text pointers inside Git, while storing the file contents on a remote server like GitHub.com or GitHub Enterprise. </p>


1. Install Git LFS via Terminal Shell Command: <br>
<code> $ brew install git-lfs </code>

2. Once downloaded and installed, set up Git LFS for your user account by running: <br>
<code> $ git lfs install </code>

3. In each Git repository where you want to use Git LFS, select the file types you'd like Git LFS to manage (or directly edit your hidden file .gitattributes). You can configure additional file extensions at anytime. Example: <br>

- <code> $ git track "*.psd" </code> <br>

- <code> $ git track "*.png" </code> <br>

- <code> $ git track "*.fbx" </code> <br>

*Make sure Git Attributes is checked:*
<code> $ git add .gitattributes </code>

4. Add all the files (will take a while to load): <br>
<code> $ git add --all </code>

5. Commit files (update files): <br>
<code> $ git commit -m "Type Comment Here" </code>
  
6. Push to Remote Repository (upload files to Github): <br>
<code> $ git push -u origin master </code>
  
<br> 

## For more information regarding Git LFS:

### What is Git LFS?
[![What is Git LFS](https://img.youtube.com/vi/9gaTargV5BY/0.jpg)](https://www.youtube.com/watch?v=9gaTargV5BY)

### How to use Git LFS (Youtube Walkthrough)
[![How to upload large files in Github](https://img.youtube.com/vi/W4RCeVSs1Fg/0.jpg)](https://www.youtube.com/watch?v=W4RCeVSs1Fg)

<br>

[Click here to visit official Git LFS Documentation](https://git-lfs.github.com/)

[Click here to visit alternativ Git LFS Documentation](https://www.atlassian.com/git/tutorials/git-lfs#clone-respository)
