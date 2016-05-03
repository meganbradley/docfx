# Cross Reference in Markdown

Markdown provides a [syntax](https://daringfireball.net/projects/markdown/syntax#link) to create hyperlinks.
For example, the following syntax:
```markdown
[bing](http://www.bing.com)
```
Will render to:

```html
<a href="http://www.bing.com">bing</a>
```

Here the url in the link could be either absolute url pointing to another website (`www.bing.com` in the above example),
or a relative url pointing to a local resource on the same server (for example, `/about/`).

When working with large documentation project that contains multiple files, it's often needed to link to another Markdown file using the relative path in the source directory, Markdown spec doesn't have a clear definition of how this should be supported.
What's more, there is also a common need to link to another file using a "semantic" name instead of its file path.
This is especially common in API reference docs, for example, you may want to use `System.String` to link to the topic of `String` class, without need to know it's actually located in `api/system/string.html`, because this path is usually auto generated.

In this document, we'll describe the functinalities DocFX provides for resolving file links and cross reference, which will help you to reference to other files in an efficient way.

## Link a file using relative path

In DocFX, you can link to a file using its relative path in the source directory. For example,

You have a `file1.md` and a `file2.md` under `subfolder/`:

```
/
|- subfolder/
|  \- file2.md
\- file1.md
```

You can use relative path to reference `file2.md` in `file1.md`:

```markdown
[file2](subfolder/file2.md)
```

Will render to:

```html
<a href="subfolder/file2.html">file2</a>
```

You can see the source file name (`.md`) is replaced with output file name (`.html`).

> DocFX does not simply replace the file extension here (`.md` to `.html`), it also tracks the mapping between input and output files to make sure source file path will resolve to correct output path. For example, if in the above case, `subfolder` is renamed to `subfolder2` using [file mapping](docfx.exe_user_manual.html#4-supported-name-files-file-mapping-format) in `docfx.json`, in output html, the link url will also be `subfolder2/file2.html`.

### Relative path vs. absolute path

It's recommended to always use relative path to reference another file in the same project. Relative path will be resolved during build and produce build warning if the target file does not exist.

> Tip: File must be included in `docfx.json` be processed by DocFX, so if you see a build warning about broken link but file actually exists in your file system, go and check whether this file is excluded in `docfx.json`.

You can still use absolute path (path starts with `/`) to link to another file, but DocFX won't check the link for you and keep the link as-is in the output html. So you should use the output file path as the link url. For example, in the above example, you can also write the link as follows:

```markdown
[file2](/subfolder/file2.html)
```

### Links in file includes
