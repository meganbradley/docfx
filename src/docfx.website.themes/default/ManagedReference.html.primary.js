// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./common.js');

function transform(model, _attrs, _global) {
  var namespaceItems = {
    "class":        { inClass: true,        typePropertyName: "inClass",        id: "classes" },
    "struct":       { inStruct: true,       typePropertyName: "inStruct",       id: "structs" },
    "interface":    { inInterface: true,    typePropertyName: "inInterface",    id: "interfaces" },
    "enum":         { inEnum: true,         typePropertyName: "inEnum",         id: "enums" },
    "delegate":     { inDelegate: true,     typePropertyName: "inDelegate",     id: "delegates" }
  };
  var classItems = {
    "constructor":  { inConstructor: true,  typePropertyName: "inConstructor",  id: "constructors" },
    "field":        { inField: true,        typePropertyName: "inField",        id: "fields" },
    "property":     { inProperty: true,     typePropertyName: "inProperty",     id: "properties" },
    "method":       { inMethod: true,       typePropertyName: "inMethod",       id: "methods" },
    "event":        { inEvent: true,        typePropertyName: "inEvent",        id: "events" },
    "operator":     { inOperator: true,     typePropertyName: "inOperator",     id: "operators" }
  };

  if (!model) return null;

  langs = model.langs;
  handleItem(model, model.newFileRepository);
  model.children.forEach(function(item) { handleItem(item, model.newFileRepository);});

  if (model.type) {
    switch (model.type.toLowerCase()) {
      case 'namespace':
        model.isNamespace = true;
        if (model.children) groupChildren(model, namespaceItems);
        break;
      case 'class':
      case 'interface':
      case 'struct':
      case 'delegate':
      case 'enum':
        model.isClass = true;
        if (model.children) groupChildren(model, classItems);
        model[namespaceItems[model.type.toLowerCase()].typePropertyName] = true;
        handleNamespace(model);
        break;
      default:
        break;
    }
  }

  var result = {item: model};
  if (_global) {
    result.__global = {};
    for (var key in _global) {
      if (_global.hasOwnProperty(key)) {
        result.__global[key] = _global[key];
      }
    }
  }
  if (_attrs) {
    for (var key in _attrs) {
      if (_attrs.hasOwnProperty(key)) {
        result[key] = _attrs[key];
      }
    }
    result._disableToc = model._disableToc || !_attrs._tocPath || (_attrs._navPath === _attrs._tocPath);
  }


  return result;
}

function groupChildren(model, typeChildrenItems) {
  var grouped = {};

  model.children.forEach(function (c) {
    var type = c.type.toLowerCase();
    if (!grouped.hasOwnProperty(type)) {
      grouped[type] = [];
    }
    // special handle for field
    if (type === "field" && c.syntax) {
      c.syntax.fieldValue = c.syntax.return;
      c.syntax.return = undefined;
    }
    // special handle for property
    if (type === "property" && c.syntax) {
      c.syntax.propertyValue = c.syntax.return;
      c.syntax.return = undefined;
    }
    // special handle for event
    if (type === "event" && c.syntax) {
      c.syntax.eventType = c.syntax.return;
      c.syntax.return = undefined;
    }
    grouped[type].push(c);
  })
  var children = [];
  for (var key in typeChildrenItems) {
    if (typeChildrenItems.hasOwnProperty(key) && grouped.hasOwnProperty(key)) {
      var typeChildrenItem = typeChildrenItems[key];
      var items = typeChildrenItem.children = grouped[key];
      if (items && items.length > 0) {
        children.push(typeChildrenItem);
      }
    }
  }

  model.children = children;
}

// reserve "namespace" of string for backward compatibility
// will replace "namespace" with "namespaceExpanded" of object
function handleNamespace(model) {
  model.namespaceExpanded = model.namespace;
  model.namespace = model.namespaceExpanded.uid;
}

function handleItem(vm, newFileRepository) {
  // get contribution information
  vm.docurl = common.getImproveTheDocHref(vm, newFileRepository);
  vm.sourceurl = common.getViewSourceHref(vm);

  // fill "undefined" if key not existed
  vm.summary = vm.summary;
  vm.remarks = vm.remarks;
  vm.conceptual = vm.conceptual;
  vm.syntax = vm.syntax;

  if (vm.supported_platforms) {
      vm.supported_platforms = transformDictionaryToArray(vm.supported_platforms);
  }

  if (vm.requirements) {
      var type = vm.type.toLowerCase();
      if (type == "method") {
          vm.requirements_method = transformDictionaryToArray(vm.requirements);
      } else {
          vm.requirements = transformDictionaryToArray(vm.requirements);
      }
  }

  if (vm && langs) {
      if (shouldHideTitleType(vm)) {
          vm.hideTitleType = true;
      } else {
          vm.hideTitleType = false;
      }

      if (shouldHideSubtitle(vm)) {
          vm.hideSubtitle = true;
      } else {
          vm.hideSubtitle = false;
      }
  }

  function shouldHideTitleType(vm) {
      var type = vm.type.toLowerCase();
      return ((type === 'namespace' && langs.length == 1 && (langs[0] === 'objectivec' || langs[0] === 'java' || langs[0] === 'c'))
      || ((type === 'class' || type === 'enum') && langs.length == 1 && langs[0] === 'c'));
  }

  function shouldHideSubtitle(vm) {
      var type = vm.type.toLowerCase();
      return (type === 'class' || type === 'namespace') && langs.length == 1 && langs[0] === 'c';
  }

  function transformDictionaryToArray(dic) {
    var array = [];
    for(var key in dic) {
        if (dic.hasOwnProperty(key)) {
            array.push({"name": key, "value": dic[key]})
        }
    }

    return array;
  }
}