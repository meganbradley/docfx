# Links and Cross References

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

DocFX converts it to a relative path in output folder structure:

```html
<a href="subfolder/file2.html">file2</a>
```

You can see the source file name (`.md`) is replaced with output file name (`.html`).

> DocFX does not simply replace the file extension here (`.md` to `.html`), it also tracks the mapping between input and output files to make sure source file path will resolve to correct output path. For example, if in the above case, `subfolder` is renamed to `subfolder2` using [file mapping](docfx.exe_user_manual.html#4-supported-name-files-file-mapping-format) in `docfx.json`, in output html, the link url will also be `subfolder2/file2.html`.

### Relative path vs. absolute path

It's recommended to always use relative path to reference another file in the same project. Relative path will be resolved during build and produce build warning if the target file does not exist.

> Tip: File must be included in `docfx.json` be processed by DocFX, so if you see a build warning about broken link but file actually exists in your file system, go and check whether this file is excluded in `docfx.json`.

You can still use absolute path (path starts with `/`) to link to another file, but DocFX won't check the link for you and will keep it as-is in the output HTML.
So you should use the output file path as the link url. For example, in the above example, you can also write the link as follows:

```markdown
[file2](/subfolder/file2.html)
```

Sometimes you may find it's complicated to calculate relative path between two files.
DocFX also supports path starts with `~` to represent path relative to the root directory of your project (i.e. where `docfx.json` is located).
It will also be validated and resolved during build. For example, in the above case, you can write the following links in `file2.md`:
 
```markdown
[file1](~/file1.md)

[file1](../file1.md)
```

Both will resolve to `../file1.html` in output html.

> [Automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) doesn't support relative path. If you write something like `<file.md>`, it will be treated as an HTML tag rather than a link.

### Links in file includes

If you use [file include](../spec/docfx_flavored_markdown.md#file-inclusion) to include another file, the links in included file is relative to the included file. For example, if `file1.md` includes `file2.md`:

```markdown
[!include[file2](subfolder/file2.md)]
```

All links in `file2.md` still relative to the `file2.md` itself, even when it's included by `file1.md`.

Please note the file path in include syntax are handled differently than Markdown link. You can use any valid file path in your operation system to specify included file (absolute path is also supported though it's not recommended). And it's not required included file to be included in `docfx.json`.

> Each file in `docfx.json` will build into an output file. But included files usually don't need to build into individual topics. So it's not recommended to include them in `docfx.json`. 

### Links in inline HTML

Markdown also supports to [inline HTML](https://daringfireball.net/projects/markdown/syntax#html). DocFX also supports to use relative path in inline HTML. Path in HTML link (`<a>`), image (`<img>`), script (`<script>`) and css (`<link>`) will also be resolved if they're relative path.

## Using cross reference

Besides using file path to link to another file, DocFX also allows you to give a file an ID so that you can reference this file using ID instead of its file path. This is useful in the some cases:

1. Path to file is long and difficult to memorize or changes frequently.
2. API reference documentation is usually auto generated so it's difficult to find its file path.
3. You want to reference to files in another project without need to know its file structure.

The basic syntax for cross referencing a file is:

```markdown
<xref:id_to_another_file>
```

Here we leverage the [automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) syntax in Markdown with a `xref` scheme. This link will build into:

```html
<a href="path_to_another_file">title_to_another_file</a>
```

> We also supports a shorthand form which is: `@id_to_another_file`. This will resolve to the same HTML in output.

### UID