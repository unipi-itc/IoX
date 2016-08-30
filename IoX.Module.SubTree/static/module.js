(function(baseUrl, modElement) {
  var ModulesMenu = React.createClass({
    getInitialState: function() {
      return {
        modules : []
      };
    },

    loadModulesFromServer: function () {
      Suave.EvReact.remoteCallback(
        baseUrl + 'available-modules',
        data => this.setState({ modules : data })
      )();
    },

    componentDidMount: function() {
      this.loadModulesFromServer();
    },

    render: function() {
      var items = this.state.modules.map(item => {
        if (item.Loaded && item.Browsable) {
          return (
            <li key={item.Id}>
              <a className="dynamic-menu-item" href={item.Url}>
                {item.Name}
              </a>
            </li>
          );
        }
      });
      return (
        <div className="collapse navbar-collapse">
          <ul className="nav navbar-nav">
            <li className="dropdown">
              <a className="dropdown-toggle" role="button" aria-expanded="false" href="#" data-toggle="dropdown" onClick={this.loadModulesFromServer}>
                Modules <span className="caret" />
              </a>
              <ul className="dropdown-menu" role="menu">
                {items}
                <li className="divider" />
                <li><a href="modules.html">Manage modules</a></li>
              </ul>
            </li>
          </ul>
        </div>
      );
    }
  });

  var ModuleView = React.createClass({
    componentDidMount: function() {
      loadIoxModule(this.props.url, this.refs.container);
    },

    render: function() {
      return <div className="panel-body" ref="container" />;
    }
  });

  var ModuleItem = React.createClass({
    getInitialState: function() {
      return {
        browsable: this.props.Browsable,
        loaded: this.props.Loaded,
        loading: false,
      };
    },

    loadModule: function() {
      this.setState({ loading: true });
      Suave.EvReact.remoteCallback(
        baseUrl + "load-module",
        data => this.setState({
          browsable: data.Browsable,
          loaded: data.Loaded,
          loading: false
        })
      )(this.props.Id);
    },

    renderModuleOps: function() {
      if (this.state.loading)
        return <i>Loading...</i>;
      else if (!this.state.loaded)
        return <a className="btn btn-link" style={{ padding: 0 }} onClick={this.loadModule}>Load</a>;
      else
        return <i>Loaded</i>;
    },

    renderModuleView: function() {
      if (this.state.browsable)
        return <ModuleView url={this.props.Url} />;
    },

    render: function() {
      return (
        <div className="panel panel-info">
          <div className="panel-heading">
            <div className="panel-title pull-left">
              {this.props.Name} - {this.props.Description}
            </div>
            <div className="panel-title pull-right">{this.renderModuleOps()}</div>
            <div className="clearfix"></div>
          </div>
          {this.renderModuleView()}
        </div>
      );
    }
  });

  var Module = React.createClass({
    getInitialState: function() {
      return {
        modules : []
      };
    },

    loadModulesFromServer: function() {
      Suave.EvReact.remoteCallback(
        baseUrl + 'available-modules',
        data => this.setState({ modules : data })
      )();
    },

    componentDidMount: function() {
      this.loadModulesFromServer();
    },

    render: function() {
      return (
        <div id="modulesList">
          {this.state.modules.map(item => <ModuleItem key={item.Id} {...item} />)}
        </div>
      );
    }
  });

  ReactDOM.render(<Module />, modElement);
  var menu = document.getElementById('modulesMenu');
  if (menu)
    ReactDOM.render(<ModulesMenu />, menu);
})
