(function(baseUrl, modElement) {
  var simplePOST = function(url, callback) {
    return data => {
        var request = new XMLHttpRequest();
        request.open('POST', url, true);
        request.setRequestHeader('Content-Type', 'application/json');
        request.onload = function () {
          if (request.status >= 200 && request.status < 400)
            callback(request.responseText);
          else
            console.error(url, request.status, request.statusText);
        };
        request.onerror = function () {
          return console.error(url, request.status, request.statusText);
        };
        request.send(JSON.stringify(data));
    };
  };

  var Module = React.createClass({
    getInitialState: function() {
      return {
        message: '',
        replies: []
      };
    },

    handleMessageChange: function(e) {
      this.setState({ message: e.target.value });
    },
    handleSubmit: function(e) {
      e.preventDefault();
      simplePOST(
        this.props.baseUrl + "chat",
        data => this.setState({ replies: this.state.replies.concat([data]) })
      )(this.state.message);
      this.setState({ message: '' });
    },

    componentDidMount: function() {
      simplePOST(
        this.props.baseUrl + "helo",
        data => console.log("helo -> ", data)
      )();
    },

    componentWillUnmount: function() {
      simplePOST(
        this.props.baseUrl + "bye",
        data => console.log("bye -> ", data)
      )();
    },

    render: function() {
      return (
        <div>
          <h1>Replies</h1>
          {this.state.replies.map((r, idx) => <div key={idx}>{r}</div>)}
          <form onSubmit={this.handleSubmit}>
            <input
              type="text"
              placeholder="Say something..."
              value={this.state.message}
              onChange={this.handleMessageChange}
            />
            <input type="submit" value="Send" />
          </form>
        </div>
      );
    }
  });

  ReactDOM.render(<Module baseUrl={baseUrl}/>, modElement);
})
